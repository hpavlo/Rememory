using Rememory.Helper;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace Rememory.Hooks
{
    public class GlobalMouseHook : IDisposable
    {
        private IntPtr _user32LibraryHandle;
        private IntPtr _windowsMouseHookHandle;
        private NativeHelper.HookProc _mouseHookProc;

        internal event EventHandler<MouseHookEventArgs>? MouseEvent;

        public GlobalMouseHook()
        {
            _user32LibraryHandle = IntPtr.Zero;
            _windowsMouseHookHandle = IntPtr.Zero;
            _mouseHookProc = LowLevelMouseProc;

            _user32LibraryHandle = NativeHelper.LoadLibrary("User32");
            if (_user32LibraryHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        public void AddMouseHook()
        {
            if (_windowsMouseHookHandle != IntPtr.Zero) return;
            _windowsMouseHookHandle = NativeHelper.SetWindowsHookEx(NativeHelper.WH_MOUSE_LL, _mouseHookProc, _user32LibraryHandle, 0);
            if (_windowsMouseHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust mouse hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        public void RemoveMouseHook()
        {
            if (_windowsMouseHookHandle != IntPtr.Zero)
            {
                if (!NativeHelper.UnhookWindowsHookEx(_windowsMouseHookHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to remove mouse hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }

                _windowsMouseHookHandle = IntPtr.Zero;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveMouseHook();
                _mouseHookProc -= LowLevelMouseProc;
            }

            if (_user32LibraryHandle != IntPtr.Zero)
            {
                if (!NativeHelper.FreeLibrary(_user32LibraryHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }

                _user32LibraryHandle = IntPtr.Zero;
            }
        }

        ~GlobalMouseHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public enum MouseMessage
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public PointInt32 pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var wparamTyped = wParam.ToInt32();
                if (Enum.IsDefined(typeof(MouseMessage), wparamTyped))
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var eventArgs = new MouseHookEventArgs(hookStruct.pt, (MouseMessage)wparamTyped);
                    MouseEvent?.Invoke(this, eventArgs);
                }
            }

            return NativeHelper.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }

    public class MouseHookEventArgs : EventArgs
    {
        public PointInt32 Position { get; }
        public GlobalMouseHook.MouseMessage Message { get; }

        public MouseHookEventArgs(PointInt32 position, GlobalMouseHook.MouseMessage message)
        {
            Position = position;
            Message = message;
        }
    }
}
