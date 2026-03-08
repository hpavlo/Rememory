using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.Views.Settings;
using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;

namespace Rememory.Views
{
    public class ClipboardWindow : WindowEx
    {
        private const uint TrayIconId = 0;
        private static readonly int WM_TASKBARCREATED = NativeHelper.RegisterWindowMessage("TaskbarCreated");

        public SettingsContext SettingsContext => SettingsContext.Instance;
        private MenuFlyout? TitleBarContextMenu => field ??= _rootPage?.Resources["TitleBarContextMenuFlyout"] as MenuFlyout;

        public readonly bool IsRoundedCornerSupported;
        private readonly InputNonClientPointerSource _inputNonClientPointerSource;
        private readonly WindowMessageMonitor _messageMonitor;

        private bool _pinned = false;
        private ClipboardRootPage? _rootPage;

        public IntPtr Handle { get; private set; }
        public TrayIcon TrayIcon { get; private set; }
        public MenuFlyout? TrayIconMenu { get; private set; }

        public bool Pinned
        {
            get => IsAlwaysOnTop && _pinned;
            set {
                int borderColor = (_pinned = value) ? NativeHelper.DWMWA_COLOR_NONE : NativeHelper.DWMWA_COLOR_DEFAULT;
                NativeHelper.DwmSetWindowAttribute(Handle, NativeHelper.DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
        }

        public event TypedEventHandler<ClipboardWindow, EventArgs>? Showing;
        public event TypedEventHandler<ClipboardWindow, EventArgs>? Hiding;

        public ClipboardWindow()
        {
            Title = Package.Current.DisplayName;
            IsShownInSwitchers = false;
            IsResizable = false;
            IsMaximizable = false;
            IsMinimizable = false;
            MinWidth = SettingsContext.WindowWidthLowerBound;
            MaxWidth = SettingsContext.WindowWidthUpperBound;
            MinHeight = SettingsContext.WindowHeightLowerBound;
            MaxHeight = SettingsContext.WindowHeightUpperBound;
            AppWindow.SetTaskbarIcon(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");
            AppWindow.SetTitleBarIcon(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");

            this.SetWindowStyle(WindowStyle.Popup);
            int cornerPreference = (int)NativeHelper.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            nint dwmResult = NativeHelper.DwmSetWindowAttribute(this.GetWindowHandle(), NativeHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            IsRoundedCornerSupported = dwmResult == 0;

            Handle = WindowNative.GetWindowHandle(this);
            TrayIcon = CreateTrayIcon();

            _messageMonitor = new WindowMessageMonitor(this.GetWindowHandle());
            _messageMonitor.WindowMessageReceived += WindowMessageReceived;

            _inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
            _inputNonClientPointerSource.ExitedMoveSize += InputNonClientPointerSource_ExitedMoveSize;

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

        public void InitWindowContent()
        {
            _rootPage = new ClipboardRootPage();
            Content = _rootPage;

            if (WindowBackdropHelper.IsSystemBackdropSupported)
            {
                var backdropHelper = new WindowBackdropHelper(this);
                backdropHelper.TryInitializeBackdrop();
            }

            _rootPage.ActualThemeChanged += ClipboardWindow_ActualThemeChanged;
            _rootPage.WindowCaptionArea.SizeChanged += WindowCaptionArea_SizeChanged;
        }

        private void ClipboardWindow_ActualThemeChanged(FrameworkElement sender, object args) => App.Current.ThemeService.ApplyTheme();

        private void WindowCaptionArea_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateCaptionRegion();

        private void WindowMessageReceived(object? sender, WindowMessageEventArgs args)
        {
            switch (args.Message.MessageId)
            {
                case NativeHelper.WM_DPICHANGED:
                    var rect = Marshal.PtrToStructure<NativeHelper.Rect>(args.Message.LParam);
                    UpdateResizeRegions(new(rect.right - rect.left, rect.bottom - rect.top));
                    UpdateCaptionRegion();
                    break;
                case NativeHelper.WM_NCLBUTTONDBLCLK:   // Double click on caption area
                    if (args.Message.WParam == 2   // HTCAPTION
                        && (_rootPage?.ViewModel.ToggleWindowPinnedCommand.CanExecute(null) ?? false))
                    {
                        _rootPage?.ViewModel.ToggleWindowPinnedCommand.Execute(null);
                    }
                    args.Handled = true;
                    break;
                case NativeHelper.WM_NCRBUTTONUP:
                    if (args.Message.WParam == 2)   // HTCAPTION
                    {
                        int x = (short)(args.Message.LParam.ToInt32() & 0xFFFF);
                        int y = (short)((args.Message.LParam.ToInt32() >> 16) & 0xFFFF);

                        var point = new PointInt32(x, y);
                        NativeHelper.ScreenToClient(Handle, ref point);

                        float scale = (float)GetDpiScaleFactor();
                        TitleBarContextMenu?.ShowAt(_rootPage, new(point.X / scale, point.Y / scale));
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
            if (!Pinned && args.WindowActivationState == WindowActivationState.Deactivated)
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
            _rootPage?.ActualThemeChanged -= ClipboardWindow_ActualThemeChanged;
        }

        private void InputNonClientPointerSource_ExitedMoveSize(InputNonClientPointerSource sender, ExitedMoveSizeEventArgs args)
        {
            if (args.MoveSizeOperation == MoveSizeOperation.Move)
            {
                var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
                var newPosition = AdjustWindowPositionToWorkArea(AppWindow.Position, AppWindow.Size, displayArea.WorkArea);
                AppWindow.Move(newPosition);
            }
            else
            {
                SettingsContext.WindowWidth = (int)Width;
                SettingsContext.WindowHeight = (int)Height;
                UpdateResizeRegions();
            }
        }

        private TrayIcon CreateTrayIcon()
        {
#if DEBUG
            string toltip = $"{"AppDescription".GetLocalizedResource()} (Dev)";
#else
            string toltip = "AppDescription".GetLocalizedResource();
#endif
            var trayIcon = new TrayIcon(TrayIconId, AppContext.BaseDirectory + "Assets\\WindowIcon.ico", toltip);
            trayIcon.ContextMenu += (s, a) => a.Flyout = TrayIconMenu ??= (_rootPage?.IsLoaded ?? false) ? _rootPage.TrayIconMenu : null;
            trayIcon.Selected += (s, a) => ShowWindow();
            trayIcon.IsVisible = true;
            return trayIcon;
        }

        private double GetDpiScaleFactor() => NativeHelper.GetDpiForWindow(Handle) / 96.0;

        private RectInt32 MakeRect(double x, double y, double width, double height, double scale)
        {
            return new((int)(x * scale), (int)(y * scale), (int)(width * scale), (int)(height * scale));
        }

        private void SetBorderRegion(NonClientRegionKind kind, double x, double y, double width, double height, double scale)
        {
            _inputNonClientPointerSource.SetRegionRects(kind, [MakeRect(x, y, width, height, scale)]);
        }

        private void SetBorderRegion(NonClientRegionKind kind, double x, double y, double width, double height)
        {
            _inputNonClientPointerSource.SetRegionRects(kind, [MakeRect(x, y, width, height, 1)]);
        }

        private void UpdateResizeRegions(SizeInt32? windowSize = null)
        {
            if (!SettingsContext.IsWindowResizeByMouseEnabled)
            {
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.LeftBorder);
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.TopBorder);
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.RightBorder);
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.BottomBorder);
                return;
            }

            double scale = GetDpiScaleFactor();
            double thickness = 5 * scale;
            double width = windowSize?.Width ?? AppWindow.Size.Width;
            double height = windowSize?.Height ?? AppWindow.Size.Height;

            if (SettingsContext.WindowPosition == ClipboardWindowPosition.Right)
            {
                SetBorderRegion(NonClientRegionKind.LeftBorder, 0, 0, thickness, height);
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.TopBorder);
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.RightBorder);
                _inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.BottomBorder);
            }
            else
            {
                SetBorderRegion(NonClientRegionKind.LeftBorder, 0, 0, thickness, height);
                SetBorderRegion(NonClientRegionKind.TopBorder, 0, 0, width, thickness);
                SetBorderRegion(NonClientRegionKind.RightBorder, width - thickness, 0, thickness, height);
                SetBorderRegion(NonClientRegionKind.BottomBorder, 0, height - thickness, width, thickness);
            }
        }

        private void UpdateCaptionRegion()
        {
            if (_rootPage is null)
            {
                return;
            }

            double scale = GetDpiScaleFactor();
            var captionPoint = _rootPage.WindowCaptionArea.TransformToVisual(_rootPage).TransformPoint(new(0, 0));
            SetBorderRegion(NonClientRegionKind.Caption, captionPoint.X, captionPoint.Y, _rootPage.WindowCaptionArea.ActualWidth, _rootPage.WindowCaptionArea.ActualHeight, scale);
        }

        private void MoveToStartPosition(ClipboardWindowPosition? position = null)
        {
            var workArea = NativeHelper.GetWorkAreaFromPoint(out var dpiX, out var dpiY);
            double dpiScaleX = dpiX / 96.0;   // 96 is a default DPI (scale 100%)
            double dpiScaleY = dpiY / 96.0;

            int windowWidth = SettingsContext.WindowWidth;
            int windowHeight = SettingsContext.WindowHeight;
            int windowMargin = SettingsContext.WindowMargin;

            double independedWidth = windowWidth * dpiScaleX;
            double independedHeight = windowHeight * dpiScaleY;
            double independedMarginX = windowMargin * dpiScaleX;
            double independedMarginY = windowMargin * dpiScaleY;

            //this.AppWindow.MoveAndResize - requires restart after DPI update, width and height depends on DPI
            //this.MoveAndResize - don't require restart after DPI update

            switch (position ?? SettingsContext.WindowPosition)
            {
                case ClipboardWindowPosition.Caret:
                    var caretPosition = GetPositionWindowRelativeToCaret(
                        (int)(workArea.X + workArea.Width - independedWidth - independedMarginX),
                        (int)(workArea.Y + workArea.Height - independedHeight - independedMarginY));
                    this.MoveAndResize(caretPosition.X, caretPosition.Y, windowWidth, windowHeight);
                    break;
                case ClipboardWindowPosition.Cursor:
                    NativeHelper.GetCursorPos(out var cursorPos);
                    var newCursorPositionPos = AdjustWindowPositionToWorkArea(cursorPos, AppWindow.Size, workArea);
                    this.MoveAndResize(newCursorPositionPos.X, newCursorPositionPos.Y, windowWidth, windowHeight);
                    break;
                case ClipboardWindowPosition.ScreenCenter:
                    this.MoveAndResize(
                        workArea.X + (workArea.Width - independedWidth) / 2,
                        workArea.Y + (workArea.Height - independedHeight) / 2,
                        windowWidth,
                        windowHeight);
                    break;
                case ClipboardWindowPosition.LastPosition:
                    // Check if last position is out of work area
                    if (AppWindow.Position.X >= workArea.X
                        && AppWindow.Position.X - independedWidth <= workArea.X + workArea.Width
                        && AppWindow.Position.Y >= workArea.Y
                        && AppWindow.Position.Y - independedHeight <= workArea.Y + workArea.Height)
                    {
                        this.MoveAndResize(
                            AppWindow.Position.X,
                            AppWindow.Position.Y,
                            windowWidth,
                            windowHeight);
                    }
                    else
                    {
                        this.MoveAndResize(
                            workArea.X + (workArea.Width - independedWidth) / 2,
                            workArea.Y + (workArea.Height - independedHeight) / 2,
                            windowWidth,
                            windowHeight);
                    }
                    break;
                case ClipboardWindowPosition.Right:
                    this.MoveAndResize(
                        workArea.X + workArea.Width - independedWidth - independedMarginX,
                        workArea.Y + independedMarginY,
                        windowWidth,
                        (workArea.Height - 2 * independedMarginY) / dpiScaleY);
                    break;
                case ClipboardWindowPosition.RightCorner:
                    this.MoveAndResize(
                        workArea.X + workArea.Width - independedWidth - independedMarginX,
                        workArea.Y + workArea.Height - independedHeight - independedMarginY,
                        windowWidth,
                        windowHeight);
                    break;
            }

            UpdateResizeRegions();
        }

        private PointInt32 AdjustWindowPositionToWorkArea(PointInt32 position, SizeInt32 size, RectInt32 workArea)
        {
            int deltaX = 0;
            int deltaY = 0;

            if (position.X < workArea.X)
            {
                deltaX = workArea.X - position.X;
            }
            if (position.Y < workArea.Y)
            {
                deltaY = workArea.Y - position.Y;
            }

            if (position.X + size.Width > workArea.X + workArea.Width)
            {
                deltaX = workArea.X + workArea.Width - position.X - size.Width;
            }
            if (position.Y + size.Height > workArea.Y + workArea.Height)
            {
                deltaY = workArea.Y + workArea.Height - position.Y - size.Height;
            }

            // return new position only if there is enough space
            if (size.Width < workArea.Width && size.Height < workArea.Height)
            {
                return new(position.X + deltaX, position.Y + deltaY);
            }

            return position;
        }

        private PointInt32 GetPositionWindowRelativeToCaret(int defaultPositionX, int defaultPositionY)
        {
            int x = defaultPositionX;
            int y = defaultPositionY;

            if (TextBoxCaretHelper.GetCaretPosition(out var caretRect))
            {
                var workArea = NativeHelper.GetWorkAreaFromPoint(out var dpiX, out var dpiY, new(caretRect.X, caretRect.Y));
                var independedWidth = (int)(SettingsContext.WindowWidth * dpiX / 96.0);
                var independedHeight = (int)(SettingsContext.WindowHeight * dpiY / 96.0);

                if (workArea.X + workArea.Width - (caretRect.X + caretRect.Width) > independedWidth)
                {
                    x = caretRect.X + caretRect.Width;
                }
                else if (workArea.X + workArea.Width - caretRect.X > independedWidth)
                {
                    x = caretRect.X;
                }
                else
                {
                    x = workArea.X + workArea.Width - independedWidth;
                }

                if (workArea.Y + workArea.Height - (caretRect.Y + caretRect.Height) > independedHeight)
                {
                    y = caretRect.Y + caretRect.Height;
                }
                else if (caretRect.Y - workArea.Y > independedHeight)
                {
                    y = caretRect.Y - independedHeight;
                }
                else if (workArea.Y + workArea.Height - caretRect.Y > independedHeight)
                {
                    y = caretRect.Y;
                }
                else
                {
                    y = workArea.Y + workArea.Height - independedHeight;
                }
            }

            return new(x, y);
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
