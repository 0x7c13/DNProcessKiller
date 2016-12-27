
namespace ProcessKiller
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Diagnostics;

    public class WinEventHelper: IDisposable
    {
        public event WindowEventHandler OnWindowForegroundChanged, OnWindowMinimizeStart, OnWindowMinimizeEnd;
        public delegate void WindowEventHandler(uint pid);
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private enum SystemEvents : uint
        {
            EVENT_SYSTEM_DESTROY = 0x8001,
            EVENT_SYSTEM_MINIMIZESTART = 0x0016,
            EVENT_SYSTEM_MINIMIZEEND = 0x0017,
            EVENT_SYSTEM_FOREGROUND = 0x0003
        }

        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("User32.dll")]
        public static extern IntPtr UnhookWindowsHookEx(IntPtr hHook);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        private readonly IntPtr _hook;
        private bool _disposed;
        private readonly WinEventDelegate _winEventDelegate;

        public WinEventHelper()
        {
            _winEventDelegate = new WinEventDelegate(WinEventProc);;
            _hook = SetWinEventHook((uint)SystemEvents.EVENT_SYSTEM_FOREGROUND, (uint)SystemEvents.EVENT_SYSTEM_DESTROY, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        public static uint GetForegroundWindowThreadProcessId()
        {
            uint procId = 0;
            GetWindowThreadProcessId(GetForegroundWindow(), out procId);
            return procId;
        }

        public static void BringProcessToFront(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_disposed) return;

            switch (eventType)
            {
                case (uint)SystemEvents.EVENT_SYSTEM_DESTROY:
                    break;
                case (uint)SystemEvents.EVENT_SYSTEM_FOREGROUND:
                    {
                        uint pid;
                        GetWindowThreadProcessId(hwnd, out pid);
                        OnWindowForegroundChanged?.Invoke(pid);
                        break;
                    }
                case (uint)SystemEvents.EVENT_SYSTEM_MINIMIZESTART:
                    {
                        uint pid;
                        GetWindowThreadProcessId(hwnd, out pid);
                        OnWindowMinimizeStart?.Invoke(pid);
                        break;
                    }
                case (uint)SystemEvents.EVENT_SYSTEM_MINIMIZEEND:
                    {
                        uint pid;
                        GetWindowThreadProcessId(hwnd, out pid);
                        OnWindowMinimizeEnd?.Invoke(pid);
                        break;
                    }
                default:
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            UnhookWindowsHookEx(_hook);
            _disposed = true;
        }

        ~WinEventHelper()
        {
            Dispose();
        }
    }
}
