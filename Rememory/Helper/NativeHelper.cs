using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Windows.Graphics;

namespace Rememory.Helper
{
    public static class NativeHelper
    {
        public const uint WM_QUERYENDSESSION = 0x0011;
        public const uint WM_ENDSESSION = 0x16;
        public const uint WM_COMMAND = 0x0111;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_USER = 0x0400;

        internal static uint CF_UNICODETEXT = 0x000D;
        internal static uint CF_BITMAP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAP
        {
            public Int32 bmType;
            public Int32 bmWidth;
            public Int32 bmHeight;
            public Int32 bmWidthBytes;
            public UInt16 bmPlanes;
            public UInt16 bmBitsPixel;
            public IntPtr bmBits;
        }

        [DllImport("user32.dll")]
        internal static extern uint RegisterClipboardFormat(string format);

        [DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint RegisterApplicationRestart(string pwzCommandline, int dwFlags);


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern bool FreeLibrary(IntPtr hModule);


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        internal static extern bool SetEvent(IntPtr hEvent);

        [DllImport("ole32.dll")]
        internal static extern uint CoWaitForMultipleObjects(
            uint dwFlags, uint dwMilliseconds, ulong nHandles,
            IntPtr[] pHandles, out uint dwIndex);


        internal const int WH_KEYBOARD_LL = 13;

        internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);


        internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        internal const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);


        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out PointInt32 lpPoint);

        internal const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] MonitorInfoEx info);

        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] int dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

        [DllImport("User32.dll")]
        internal static extern IntPtr MonitorFromPoint(PointInt32 pt, int flags);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        internal class MonitorInfoEx
        {
            public int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
            public Rect rcMonitor;
            public Rect rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }

        /// <param name="dpiX">dpiX of the monitor</param>
        /// <param name="dpiY">dpiY of the monitor</param>
        /// <returns><see cref="Rectangle"/> of the monitor work area where the cursor is currently located</returns>
        internal static Rectangle GetWorkAreaRectangle(out uint dpiX, out uint dpiY)
        {
            GetCursorPos(out var point);
            IntPtr monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
            MonitorInfoEx info = new();
            GetMonitorInfo(monitor, info);

            GetDpiForMonitor(monitor, 0, out dpiX, out dpiY);   // Get MDT_EFFECTIVE_DPI
            return new Rectangle(
                info.rcWork.left,
                info.rcWork.top,
                info.rcWork.right - info.rcWork.left,
                info.rcWork.bottom - info.rcWork.top);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint numberOfInputs, KeyboardHelper.INPUT[] inputs, int sizeOfInputStructure);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        internal static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);


        internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        internal enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("Dwmapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
