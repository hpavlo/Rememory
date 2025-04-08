using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.Views.Settings;
using System;
using System.Drawing;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.System;
using WinUIEx;
using WinUIEx.Messaging;

namespace Rememory.Views
{
    public class ClipboardWindow : WindowEx
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;
        public bool IsPinned { get; set; } = false;

        public event TypedEventHandler<ClipboardWindow, EventArgs>? Showing;
        public event TypedEventHandler<ClipboardWindow, EventArgs>? Hiding;

        private WindowMessageMonitor _messageMonitor;

        public ClipboardWindow()
        {
            Title = Package.Current.DisplayName;
            IsShownInSwitchers = false;
            IsAlwaysOnTop = true;
            IsResizable = false;
            IsMaximizable = false;
            IsMinimizable = false;
            TaskBarIcon = Icon.FromFile(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");

            this.SetWindowStyle(WindowStyle.Popup);
            int cornerPreference = (int)NativeHelper.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            NativeHelper.DwmSetWindowAttribute(this.GetWindowHandle(), NativeHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            _messageMonitor = new WindowMessageMonitor(this.GetWindowHandle());
            _messageMonitor.WindowMessageReceived += WindowMessageReceived;

            this.Activated += Window_Activated;
            this.AppWindow.Closing += Window_Closing;
            this.Closed += Window_Closed;
        }

        public bool ShowWindow()
        {
            if (this.Visible)
            {
                this.SetForegroundWindow();
                return false;
            }
            MoveToStartPosition();
            Showing?.Invoke(this, EventArgs.Empty);
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
            Hiding?.Invoke(this, EventArgs.Empty);
            this.AppWindow.Hide();
            return true;
        }

        // Call it after set Content of window
        public bool InitSystemBackdrop()
        {
            if (WindowBackdropHelper.IsSystemBackdropSupported)
            {
                var backdropHelper = new WindowBackdropHelper(this);
                return backdropHelper.TryInitializeBackdrop();
            }
            return false;
        }

        private void WindowMessageReceived(object? sender, WindowMessageEventArgs args)
        {
            switch (args.Message.MessageId)
            {
                case NativeHelper.WM_QUERYENDSESSION:
                    if (args.Message.LParam == 1)   // ENDSESSION_CLOSEAPP
                    {
                        NativeHelper.RegisterApplicationRestart(string.Empty, 0x1011);   // RESTART_NO_CRASH  | RESTART_NO_HANG  | RESTART_NO_REBOOT
                    }
                    args.Result = 1;
                    args.Handled = true;
                    break;

                case NativeHelper.WM_ENDSESSION:
                    if (args.Message.WParam != 0)   // wParam = 1 means the session is ending
                    {
                        App.Current.Exit();
                    }
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
                            App.Current.Exit();
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
            if (!IsPinned && args.WindowActivationState == WindowActivationState.Deactivated)
            {
                HideWindow();
            }
        }

        private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            HideWindow();
            args.Cancel = true;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SettingsWindow.CloseSettingsWindow();
            this.Activated -= Window_Activated;
            _messageMonitor.WindowMessageReceived -= WindowMessageReceived;
        }

        private void MoveToStartPosition()
        {
            var workArea = NativeHelper.GetWorkAreaRectangle(out var dpiX, out var dpiY);
            double dpiScaleX = dpiX / 96.0;   // 96 is a default DPI (scale 100%)
            double dpiScaleY = dpiY / 96.0;

            int scaledWidth = SettingsContext.WindowWidth;
            int scaledHeight = SettingsContext.WindowHeight;
            int scaledMargin = SettingsContext.WindowMargin;

            double independedWidth = scaledWidth * dpiScaleX;
            double independedHeight = scaledHeight * dpiScaleY;
            double independedMarginX = scaledMargin * dpiScaleX;
            double independedMarginY = scaledMargin * dpiScaleY;

            //this.AppWindow.MoveAndResize - requires restart after DPI update, width and height depends on DPI
            //this.MoveAndResize - don't require restart after DPI update

            switch ((ClipboardWindowPosition)SettingsContext.ClipboardWindowPositionIndex)
            {
                case ClipboardWindowPosition.Caret:
                    PositionWindowRelativeToCaret(
                        workArea,
                        scaledWidth,
                        scaledHeight,
                        (int)(workArea.Right - independedWidth - independedMarginX),
                        (int)(workArea.Bottom - independedHeight - independedMarginY));
                    break;
                case ClipboardWindowPosition.ScreenCenter:
                    this.MoveAndResize(
                        workArea.Left + (workArea.Width - independedWidth) / 2,
                        workArea.Top + (workArea.Height - independedHeight) / 2,
                        scaledWidth,
                        scaledHeight);
                    break;
                case ClipboardWindowPosition.LastPosition:
                    // Check if last position is out of work area
                    if (this.AppWindow.Position.X >= workArea.Left
                        && this.AppWindow.Position.X - independedWidth <= workArea.Right
                        && this.AppWindow.Position.Y >= workArea.Top
                        && this.AppWindow.Position.Y - independedHeight <= workArea.Bottom)
                    {
                        this.MoveAndResize(
                            this.AppWindow.Position.X,
                            this.AppWindow.Position.Y,
                            scaledWidth,
                            scaledHeight);
                    }
                    else
                    {
                        this.MoveAndResize(
                            workArea.Left + (workArea.Width - independedWidth) / 2,
                            workArea.Top + (workArea.Height - independedHeight) / 2,
                            scaledWidth,
                            scaledHeight);
                    }
                    break;
                case ClipboardWindowPosition.Right:
                    this.MoveAndResize(
                        workArea.Right - independedWidth - independedMarginX,
                        workArea.Top + independedMarginY,
                        scaledWidth,
                        (workArea.Height - 2 * independedMarginY) / dpiScaleY);
                    break;
                case ClipboardWindowPosition.RightCorner:
                    this.MoveAndResize(
                        workArea.Right - independedWidth - independedMarginX,
                        workArea.Bottom - independedHeight - independedMarginY,
                        scaledWidth,
                        scaledHeight);
                    break;
            }
        }

        private void PositionWindowRelativeToCaret(Rectangle workArea, int windowWidth, int windowHeight, int defaultPositionX, int defaultPositionY)
        {
            int x = defaultPositionX;
            int y = defaultPositionY;

            TextBoxCaretHelper.GetCaretPosition(out var caretRect);

            if (!caretRect.IsEmpty)
            {
                if (workArea.Right - caretRect.Right > windowWidth)
                {
                    x = caretRect.Right;
                }
                else if (workArea.Right - caretRect.Left > windowWidth)
                {
                    x = caretRect.Left;
                }
                else
                {
                    x = workArea.Right - windowWidth;
                }

                if (workArea.Bottom - caretRect.Bottom > windowHeight)
                {
                    y = caretRect.Bottom;
                }
                else if (caretRect.Top - workArea.Top > windowHeight)
                {
                    y = caretRect.Top - windowHeight;
                }
                else if (workArea.Bottom - caretRect.Top > windowHeight)
                {
                    y = caretRect.Top;
                }
                else
                {
                    y = workArea.Bottom - windowHeight;
                }
            }

            this.MoveAndResize(x, y, windowWidth, windowHeight);
        }
    }

    public enum ClipboardWindowPosition
    {
        Caret,
        ScreenCenter,
        LastPosition,
        Right,
        RightCorner
    }
}
