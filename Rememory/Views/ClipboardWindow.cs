using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.Views.Settings;
using System;
using System.Drawing;
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
        public readonly bool IsRoundedCornerSupported;

        private const uint TrayIconId_ = 0;
        public TrayIcon TrayIcon { get; private set; }
        public MenuFlyout? TrayIconMenu { get; private set; }

        private bool _isPinned = false;
        public bool IsPinned
        {
            get => IsAlwaysOnTop && _isPinned;
            set {
                int borderColor = (_isPinned = value) ? NativeHelper.DWMWA_COLOR_NONE : NativeHelper.DWMWA_COLOR_DEFAULT;
                NativeHelper.DwmSetWindowAttribute(this.GetWindowHandle(), NativeHelper.DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
        }

        public event TypedEventHandler<ClipboardWindow, EventArgs>? Showing;
        public event TypedEventHandler<ClipboardWindow, EventArgs>? Hiding;

        private static int WM_TASKBARCREATED = NativeHelper.RegisterWindowMessage("TaskbarCreated");
        private WindowMessageMonitor _messageMonitor;

        public ClipboardWindow()
        {
            Title = Package.Current.DisplayName;
            IsShownInSwitchers = false;
            IsResizable = false;
            IsMaximizable = false;
            IsMinimizable = false;
            AppWindow.SetTaskbarIcon(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");
            AppWindow.SetTitleBarIcon(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");

            this.SetWindowStyle(WindowStyle.Popup);
            int cornerPreference = (int)NativeHelper.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            nint dwmResult = NativeHelper.DwmSetWindowAttribute(this.GetWindowHandle(), NativeHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            IsRoundedCornerSupported = dwmResult == 0;

            TrayIcon = CreateTrayIcon();

            _messageMonitor = new WindowMessageMonitor(this.GetWindowHandle());
            _messageMonitor.WindowMessageReceived += WindowMessageReceived;

            Activated += Window_Activated;
            AppWindow.Closing += Window_Closing;
            Closed += Window_Closed;
        }

        public bool ShowWindow()
        {
            if (Visible)
            {
                MoveToStartPosition();
                this.SetForegroundWindow();
                return false;
            }
            MoveToStartPosition();
            Showing?.Invoke(this, EventArgs.Empty);
            AppWindow.Show();
            IsAlwaysOnTop = true;
            KeyboardHelper.MultiKeyAction([(VirtualKey)0x0E], KeyboardHelper.KeyAction.DownUp);   // To fix problem with foreground window
            this.SetForegroundWindow();
            return true;
        }

        public bool HideWindow()
        {
            if (!Visible)
            {
                return false;
            }
            Hiding?.Invoke(this, EventArgs.Empty);
            AppWindow.Hide();
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

        // Call it after set Content of window
        public void InitSystemThemeTrigger()
        {
            ((FrameworkElement)Content).ActualThemeChanged += ClipboardWindow_ActualThemeChanged;
        }

        public static PointInt32 AdjustWindowPositionToWorkArea(PointInt32 position, SizeInt32 size, Rectangle? workArea = null)
        {
            var workAreaRect = workArea ?? NativeHelper.GetWorkAreaRectangle(out _, out _);
            int deltaX = 0;
            int deltaY = 0;

            // Adjust horisontal position
            if (position.X < workAreaRect.Left)
            {
                deltaX = workAreaRect.Left - position.X;
            }
            if (position.Y < workAreaRect.Top)
            {
                deltaY = workAreaRect.Top - position.Y;
            }

            // Adjust vertical position
            if (position.X + size.Width > workAreaRect.Right)
            {
                deltaX = workAreaRect.Right - position.X - size.Width;
            }
            if (position.Y + size.Height > workAreaRect.Bottom)
            {
                deltaY = workAreaRect.Bottom - position.Y - size.Height;
            }

            // return new position only if there is enough space
            if (size.Width < workAreaRect.Width && size.Height < workAreaRect.Height)
            {
                return new(position.X + deltaX, position.Y + deltaY);
            }

            return position;
        }

        private void ClipboardWindow_ActualThemeChanged(FrameworkElement sender, object args) => App.Current.ThemeService.ApplyTheme();

        private void WindowMessageReceived(object? sender, WindowMessageEventArgs args)
        {
            switch (args.Message.MessageId)
            {
                case NativeHelper.WM_SETTINGCHANGE:
                    if (args.Message.WParam == NativeHelper.SPI_SETLOGICALDPIOVERRIDE)   // Update position and size on DPI update
                    {
                        MoveToStartPosition();
                    }
                    break;

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
                default:
                    if (args.Message.MessageId == WM_TASKBARCREATED)
                    {
                        TrayIcon.IsVisible = false;
                        TrayIcon.IsVisible = true;
                    }
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
            Activated -= Window_Activated;
            AppWindow.Closing -= Window_Closing;
            Closed -= Window_Closed;
            _messageMonitor.WindowMessageReceived -= WindowMessageReceived;
            ((FrameworkElement)Content).ActualThemeChanged -= ClipboardWindow_ActualThemeChanged;
        }

        private TrayIcon CreateTrayIcon()
        {
#if DEBUG
            string toltip = $"{"AppDescription".GetLocalizedResource()} (Dev)";
#else
            string toltip = "AppDescription".GetLocalizedResource();
#endif
            var trayIcon = new TrayIcon(TrayIconId_, AppContext.BaseDirectory + "Assets\\WindowIcon.ico", toltip);
            trayIcon.ContextMenu += (s, a) => a.Flyout = TrayIconMenu ??= (Content is ClipboardRootPage rootPage && rootPage.IsLoaded ? rootPage.TrayIconMenu : null);
            trayIcon.Selected += (s, a) => ShowWindow();
            trayIcon.IsVisible = true;
            return trayIcon;
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

            switch (SettingsContext.WindowPosition)
            {
                case ClipboardWindowPosition.Caret:
                    PositionWindowRelativeToCaret(
                        workArea,
                        scaledWidth,
                        scaledHeight,
                        (int)(workArea.Right - independedWidth - independedMarginX),
                        (int)(workArea.Bottom - independedHeight - independedMarginY));
                    break;
                case ClipboardWindowPosition.Cursor:
                    NativeHelper.GetCursorPos(out var cursorPos);
                    var newPos = AdjustWindowPositionToWorkArea(cursorPos, new((int)independedWidth, (int)independedHeight), workArea);
                    this.MoveAndResize(newPos.X, newPos.Y, scaledWidth, scaledHeight);
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
                    if (AppWindow.Position.X >= workArea.Left
                        && AppWindow.Position.X - independedWidth <= workArea.Right
                        && AppWindow.Position.Y >= workArea.Top
                        && AppWindow.Position.Y - independedHeight <= workArea.Bottom)
                    {
                        this.MoveAndResize(
                            AppWindow.Position.X,
                            AppWindow.Position.Y,
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
        Cursor,
        ScreenCenter,
        LastPosition,
        Right,
        RightCorner
    }
}
