
namespace ProcessKiller
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class KeyboardInputEventHelper : IDisposable
    {
        public event EventHandler<KeyEventArgs> KeyBoardKeyDownEvent;

        public delegate IntPtr HookDelegate(
            Int32 Code, IntPtr wParam, IntPtr lParam);

        private const int WM_KEYDOWN = 0x0100;

        [DllImport("User32.dll")]
        public static extern IntPtr SetWindowsHookEx(
            Int32 idHook, HookDelegate lpfn, IntPtr hmod,
            Int32 dwThreadId);

        [DllImport("User32.dll")]
        public static extern IntPtr CallNextHookEx(
          IntPtr hHook, Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr UnhookWindowsHookEx(IntPtr hHook);

        private HookDelegate _keyBoardDelegate;
        private readonly IntPtr _keyBoardHandle;
        private const Int32 WH_KEYBOARD_LL = 13;
        private bool _disposed;

        public KeyboardInputEventHelper()
        {
            _keyBoardDelegate = KeyboardHookDelegate;
            _keyBoardHandle = SetWindowsHookEx(
                WH_KEYBOARD_LL, _keyBoardDelegate, IntPtr.Zero, 0);
        }

        public struct KeyPressInfo
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private IntPtr KeyboardHookDelegate(
            Int32 Code, IntPtr wParam, IntPtr lParam)
        {
            var kpi = (KeyPressInfo)Marshal.PtrToStructure(lParam, typeof(KeyPressInfo));
            var key = (Keys)kpi.vkCode;
            //var keyArgs = new KeyEventArgs(key);

            if (Code < 0)
            {
                return CallNextHookEx(
                    _keyBoardHandle, Code, wParam, lParam);
            }

            if (Code >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                KeyBoardKeyDownEvent?.Invoke(this, new KeyEventArgs(key));
            }

            return CallNextHookEx(
                _keyBoardHandle, Code, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_keyBoardHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyBoardHandle);
            }

            _disposed = true;
        }

        ~KeyboardInputEventHelper()
        {
            Dispose();
        }
    }
}
