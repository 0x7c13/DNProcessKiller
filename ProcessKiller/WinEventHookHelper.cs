
namespace ProcessKiller
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Diagnostics;

    public class WinEventHookHelper: IDisposable
    {
        public event ProcessEnterForegroundEventHandler ProcessEnterForeground;
        public delegate void ProcessEnterForegroundEventHandler(uint pid);
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("User32.dll")]
        public static extern IntPtr UnhookWindowsHookEx(IntPtr hHook);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private readonly IntPtr _hook;
        private bool _disposed;
        private readonly WinEventDelegate _winEventDelegate;

        public WinEventHookHelper()
        {
            _winEventDelegate = new WinEventDelegate(WinEventProc);;
            _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_disposed) return;

            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);

            if (Process.GetCurrentProcess().Id == pid)
            {
                return;
            }

            Console.WriteLine($"Process [{pid}] becomes active");
            ProcessEnterForeground?.Invoke(pid);
        }

        public void Dispose()
        {
            if (_disposed) return;
            UnhookWindowsHookEx(_hook);
            _disposed = true;
        }

        ~WinEventHookHelper()
        {
            Dispose();
        }
    }
}
