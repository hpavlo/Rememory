using Rememory.Helper;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rememory.Hooks
{
    /// <summary>
    /// Helps to get last active window when the clipboard window is pinned
    /// </summary>
    public class ActiveWindowHook : IDisposable
    {
        private IntPtr _windowsEventHookHandle;
        private NativeHelper.WinEventDelegate _windowsEventDelegate;

        public IntPtr LastActiveWindowHandle { get; private set; } = IntPtr.Zero;

        public ActiveWindowHook()
        {
            _windowsEventHookHandle = IntPtr.Zero;
            _windowsEventDelegate = ForegroundEventDelegate;
        }

        public void AddEventHook()
        {
            if (_windowsEventHookHandle != IntPtr.Zero) return;
            _windowsEventHookHandle = NativeHelper.SetWinEventHook(NativeHelper.EVENT_SYSTEM_FOREGROUND, NativeHelper.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _windowsEventDelegate, 0, 0, NativeHelper.WINEVENT_OUTOFCONTEXT | NativeHelper.WINEVENT_SKIPOWNPROCESS);
            if (_windowsEventHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust event hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        public void RemoveEventHook()
        {
            if (_windowsEventHookHandle != IntPtr.Zero)
            {
                if (!NativeHelper.UnhookWinEvent(_windowsEventHookHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to remove event hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }

                _windowsEventHookHandle = IntPtr.Zero;
                LastActiveWindowHandle = IntPtr.Zero;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // because we can unhook only in the same thread, not in garbage collector thread
                RemoveEventHook();
                _windowsEventDelegate -= ForegroundEventDelegate;
            }
        }

        ~ActiveWindowHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ForegroundEventDelegate(nint hWinEventHook, uint eventType, nint hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            LastActiveWindowHandle = hWnd;
        }
    }
}
