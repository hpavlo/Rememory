using Microsoft.UI;
using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using Rememory.Helper;
using System;
using System.Runtime.InteropServices;

namespace Rememory.Views.BriefMessage
{
    public static class BriefMessageWindow
    {
        private const string WINDOW_CLASS_NAME = "Rememory_BriefMessage";
        private const int WIDTH = 120;
        private const int HEIGHT = 14;
        private static readonly NativeHelper.WndProc delegateWindowProc = WindowProc;
        private static NativeHelper.WNDCLASSEX _windowClass;
        private static ushort _regResult;

        private static AppWindow? _appWindow;
        private static BriefMessageRootPage? _rootPage;

        public static void ShowBriefMessage(string? iconGlyph)
        {
            _rootPage?.ForceHideToolTip();

            if (_appWindow == null)
            {
                InitializeWindow();
            }
            else
            {
                CalculateWindowPosition(out int x, out int y, out _, out _);
                _appWindow.MoveAndResize(new(x, y, WIDTH, HEIGHT));
            }

            _rootPage?.OpenToolTip(iconGlyph);
        }

        private static void InitializeWindow()
        {
            if (_regResult == 0)
            {
                _regResult = CreateWindowClass();
            }

            CalculateWindowPosition(out int x, out int y, out int scaledWidth, out int scaledHeight);

            var windowHwnd = NativeHelper.CreateWindowEx(
                NativeHelper.WS_EX_TRANSPARENT | NativeHelper.WS_EX_LAYERED | NativeHelper.WS_EX_TOPMOST | NativeHelper.WS_EX_NOACTIVATE,
                _regResult,
                string.Empty,
                NativeHelper.WS_POPUP,
                x, y, scaledWidth, scaledHeight,
                IntPtr.Zero,
                IntPtr.Zero,
                _windowClass.hInstance,
                IntPtr.Zero);

            if (windowHwnd == 0)
            {
                return;
            }

            var windowId = Win32Interop.GetWindowIdFromWindow(windowHwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            WinUIHost(windowId);
        }

        private static ushort CreateWindowClass()
        {
            _windowClass = new()
            {
                cbSize = Marshal.SizeOf(typeof(NativeHelper.WNDCLASSEX)),
                style = (int)(NativeHelper.CS_HREDRAW | NativeHelper.CS_VREDRAW),
                hbrBackground = (IntPtr)NativeHelper.COLOR_BACKGROUND + 1,   // Black background, + 1 is necessary
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = Marshal.GetHINSTANCE(App.Current.GetType().Module),
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                lpszMenuName = string.Empty,
                lpszClassName = WINDOW_CLASS_NAME,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegateWindowProc),
                hIconSm = IntPtr.Zero
            };
            return NativeHelper.RegisterClassEx(ref _windowClass);
        }

        private static void WinUIHost(WindowId windowId)
        {
            var xamlSource = new DesktopWindowXamlSource();
            xamlSource.Initialize(windowId);
            xamlSource.SiteBridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
            _rootPage = new BriefMessageRootPage(xamlSource, windowId);
            xamlSource.Content = _rootPage;
        }

        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case NativeHelper.WM_DESTROY:
                    NativeHelper.DestroyWindow(hWnd);
                    break;
            }
            return NativeHelper.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private static void CalculateWindowPosition(out int x, out int y, out int scaledWidth, out int scaledHeight)
        {
            var workArea = NativeHelper.GetWorkAreaRectangle(out var dpiX, out var dpiY);
            double dpiScaleX = dpiX / 96.0;   // 96 is a default DPI (scale 100%)
            double dpiScaleY = dpiY / 96.0;

            scaledWidth = (int)(WIDTH * dpiScaleX);
            scaledHeight = (int)(HEIGHT * dpiScaleY);

            x = workArea.X + (workArea.Width - scaledWidth) / 2;
            y = workArea.Y + workArea.Height - scaledHeight;
        }
    }
}
