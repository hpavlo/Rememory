using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.Views.Settings;
using System;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using WinUIEx;
using WinUIEx.Messaging;

namespace Rememory.Views
{
    public class ClipboardWindow : WindowEx
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private WindowMessageMonitor _messageMonitor;
        private bool _queryEndSessionReceived = false;

        public ClipboardWindow()
        {
            Title = Package.Current.DisplayName;
            IsShownInSwitchers = false;
            IsAlwaysOnTop = true;
            IsResizable = false;
            IsMaximizable = false;
            IsMinimizable = false;
            TaskBarIcon = Icon.FromFile(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");
            Content = new ClipboardRootPage(this);

            this.SetWindowStyle(WindowStyle.Popup);
            int cornerPreference = (int)NativeHelper.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            NativeHelper.DwmSetWindowAttribute(this.GetWindowHandle(), NativeHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            if (WindowBackdropHelper.IsSystemBackdropSupported)
            {
                var backdropHelper = new WindowBackdropHelper(this);
                backdropHelper.InitWindowBackdrop();
            }

            _messageMonitor = new WindowMessageMonitor(this.GetWindowHandle());
            _messageMonitor.WindowMessageReceived += WindowMessageReceived;

            this.Activated += Window_Activated;
            this.AppWindow.Closing += Window_Closing;
            this.Closed += Window_Closed;
        }

        public bool ShowWindow()
        {
            if (this.Visible)
                return false;
            MoveToStartPosition();
            this.AppWindow.Show();
            KeyboardHelper.MultiKeyAction([(VirtualKey)0x0E], KeyboardHelper.KeyAction.DownUp);   // To fix problem with foreground window
            this.SetForegroundWindow();
            return true;
        }

        public bool HideWindow()
        {
            if (!this.Visible)
            {
                return false;
            }
            this.AppWindow.Hide();
            return true;
        }

        private void WindowMessageReceived(object sender, WindowMessageEventArgs args)
        {
            switch (args.Message.MessageId)
            {
                case NativeHelper.WM_QUERYENDSESSION:
                    _queryEndSessionReceived = true;
                    break;

                case NativeHelper.WM_COMMAND:
                    switch (args.Message.WParam)
                    {
                        case RememoryCoreHelper.TRAY_OPEN_COMMAND:
                            ShowWindow();
                            break;
                        case RememoryCoreHelper.TRAY_SETTINGS_COMMAND:
                            SettingsWindow.ShowSettingsWindow();
                            break;
                        case RememoryCoreHelper.TRAY_EXIT_COMMAND:
                            this.Close();
                            break;
                    }
                    break;
                case RememoryCoreHelper.TRAY_NOTIFICATION:
                    if (args.Message.LParam == NativeHelper.WM_LBUTTONUP)
                        ShowWindow();
                    break;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                HideWindow();
            }
        }

        private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (!_queryEndSessionReceived)
            {
                HideWindow();
                args.Cancel = true;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SettingsWindow.CloseSettingsWindow();
            //_keyboardMonitor.StopMonitor();
            this.Activated -= Window_Activated;
            _messageMonitor.WindowMessageReceived -= WindowMessageReceived;
            //Exit();
        }

        private void MoveToStartPosition()
        {
            var workArea = GetWorkAreaRectangle();

            int width = SettingsContext.WindowWidth;
            int margin = SettingsContext.WindowMargin;

            // To update DPI for window
            this.AppWindow.Move(new(
                (int)workArea.Right - width - margin,
                (int)workArea.Top + margin));

            // Resize window
            this.AppWindow.MoveAndResize(new RectInt32(
                (int)workArea.Right - width - margin,
                (int)workArea.Top + margin,
                width,
                (int)workArea.Height - 2 * margin));
        }

        private Rect GetWorkAreaRectangle()
        {
            NativeHelper.GetCursorPos(out NativeHelper.PointInter point);
            IntPtr monitor = NativeHelper.MonitorFromPoint(point, NativeHelper.MONITOR_DEFAULTTONEAREST);
            NativeHelper.MonitorInfoEx info = new();
            NativeHelper.GetMonitorInfo(monitor, info);

            return new Rect(
                info.rcWork.left,
                info.rcWork.top,
                info.rcWork.right - info.rcWork.left,
                info.rcWork.bottom - info.rcWork.top);
        }
    }
}
