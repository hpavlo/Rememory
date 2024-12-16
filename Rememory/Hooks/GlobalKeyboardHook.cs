using Rememory.Helper;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rememory.Hooks
{
    public class GlobalKeyboardHook : IDisposable
    {
        private IntPtr _user32LibraryHandle;
        private IntPtr _windowsKeyboardHookHandle;
        private NativeHelper.HookProc _keyboardHookProc;

        internal event EventHandler<GlobalKeyboardHookEventArgs> KeyboardHandler;

        public GlobalKeyboardHook()
        {
            _user32LibraryHandle = IntPtr.Zero;
            _windowsKeyboardHookHandle = IntPtr.Zero;
            _keyboardHookProc = LowLevelKeyboardProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.

            _user32LibraryHandle = NativeHelper.LoadLibrary("User32");
            if (_user32LibraryHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        public void AddKeyboardHook()
        {
            if (_windowsKeyboardHookHandle != IntPtr.Zero) return;
            _windowsKeyboardHookHandle = NativeHelper.SetWindowsHookEx(NativeHelper.WH_KEYBOARD_LL, _keyboardHookProc, _user32LibraryHandle, 0);
            if (_windowsKeyboardHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        public void RemoveKeyboardHook()
        {
            if (_windowsKeyboardHookHandle != IntPtr.Zero)
            {
                if (!NativeHelper.UnhookWindowsHookEx(_windowsKeyboardHookHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }

                _windowsKeyboardHookHandle = IntPtr.Zero;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // because we can unhook only in the same thread, not in garbage collector thread
                RemoveKeyboardHook();
                _keyboardHookProc -= LowLevelKeyboardProc;
            }

            if (_user32LibraryHandle != IntPtr.Zero)
            {
                // reduces reference to library by 1.
                if (!NativeHelper.FreeLibrary(_user32LibraryHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }

                _user32LibraryHandle = IntPtr.Zero;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public enum KeyboardState
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105
        }

        private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool isHandled = false;

            var wparamTyped = wParam.ToInt32();
            if (Enum.IsDefined(typeof(KeyboardState), wparamTyped))
            {
                object o = Marshal.PtrToStructure(lParam, typeof(KeyboardHelper.LowLevelKeyboardInputEvent));
                KeyboardHelper.LowLevelKeyboardInputEvent p = (KeyboardHelper.LowLevelKeyboardInputEvent)o;

                var eventArguments = new GlobalKeyboardHookEventArgs(p, (KeyboardState)wparamTyped);

                EventHandler<GlobalKeyboardHookEventArgs> handler = KeyboardHandler;
                handler?.Invoke(this, eventArguments);

                isHandled = eventArguments.Handled;
            }

            return isHandled ? 1 : NativeHelper.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}
