using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Hooks;
using Rememory.Models;
using Rememory.Views.Settings;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;
using Microsoft.UI.Dispatching;

namespace Rememory.Views
{
    public class ClipboardWindow : WindowEx
    {
        private const uint TrayIconId = 0;
        private const int TrayIconDoubleClickDelay = 250;
        private static readonly int WM_TASKBARCREATED = NativeHelper.RegisterWindowMessage("TaskbarCreated");

        public SettingsContext SettingsContext { get; } = App.Current.SettingsContext;
        private MenuFlyout? TitleBarContextMenu => field ??= _rootPage?.Resources["TitleBarContextMenuFlyout"] as MenuFlyout;

        public readonly bool IsRoundedCornerSupported;
        private readonly InputNonClientPointerSource _inputNonClientPointerSource;
        private readonly WindowMessageMonitor _messageMonitor;
        private readonly GlobalMouseHook _globalMouseHook = new();

        private bool _pinned = false;
        /// <summary>
        /// Tracks whether a double‑click has been detected during the current cycle.
        /// If true, any pending single‑click action should be suppressed.
        /// </summary>
        private bool _trayIconDoubleClickDetected = false;
        /// <summary>
        /// Used to block multiple Selected events from scheduling duplicate single‑click actions
        /// when the user actually performed a double‑click.
        /// </summary>
        private bool _trayIconSingleClickPending = false;
        private ClipboardRootPage? _rootPage;

        public IntPtr Handle { get; private set; }
        public TrayIcon TrayIcon { get; private set; }
        public MenuFlyout? TrayIconMenu { get; private set; }

        public bool Pinned
        {
            get => IsAlwaysOnTop && _pinned;
            set
            {
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

            // Set up mouse hook to detect clicks outside the window
            _globalMouseHook.MouseEvent += GlobalMouseHook_MouseEvent;

            Activated += Window_Activated;
            AppWindow.Closing += Window_Closing;
            Closed += Window_Closed;
        }

        public bool ShowWindow(ClipboardWindowPosition? position = null)
        {
            if (Visible)
            {
                MoveToStartPosition(position);
                this.SetForegroundWindow();
                return false;
            }
            MoveToStartPosition(position);
            Showing?.Invoke(this, EventArgs.Empty);
            AppWindow.Show(activateWindow: false);
            IsAlwaysOnTop = true;
            KeyboardHelper.MultiKeyAction([(VirtualKey)0x0E], KeyboardHelper.KeyAction.DownUp);

            _globalMouseHook.AddMouseHook();

            return true;
        }

        public bool HideWindow()
        {
            if (!Visible)
            {
                return false;
            }

            _globalMouseHook.RemoveMouseHook();

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

            TrayIcon.IsVisible = true;

            _rootPage.ActualThemeChanged += ClipboardWindow_ActualThemeChanged;
            _rootPage.WindowCaptionArea.SizeChanged += WindowCaptionArea_SizeChanged;
            _rootPage.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ClipboardWindow_ActualThemeChanged(FrameworkElement sender, object args) => App.Current.ThemeService.ApplyTheme();

        private void WindowCaptionArea_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateCaptionRegion();

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_rootPage.ViewModel.IsClipboardMonitoringEnabled))
            {
                var (iconPath, tooltip) = GetTrayIconAndTooltip();
                TrayIcon.SetIcon(iconPath);
                TrayIcon.Tooltip = tooltip;
            }
        }

        private void WindowMessageReceived(object? sender, WindowMessageEventArgs args)
        {
            switch (args.Message.MessageId)
            {
                // Update position and size on DPI update
                case NativeHelper.WM_SETTINGCHANGE when args.Message.WParam == NativeHelper.SPI_SETLOGICALDPIOVERRIDE:
                    MoveToStartPosition();
                    break;
                case NativeHelper.WM_DPICHANGED:
                    var rect = Marshal.PtrToStructure<NativeHelper.Rect>(args.Message.LParam);
                    UpdateResizeRegions(new(rect.right - rect.left, rect.bottom - rect.top));
                    UpdateCaptionRegion();
                    break;
                // Prevent window moving if it displays in "Right" position
                case NativeHelper.WM_SYSCOMMAND:
                    if ((args.Message.WParam & 0xFFF0) == 0xF010   // SC_MOVE
                        && SettingsContext.WindowPosition == ClipboardWindowPosition.Right)
                    {
                        args.Handled = true;
                    }
                    break;
                // Double click on caption area
                case NativeHelper.WM_NCLBUTTONDBLCLK when args.Message.WParam == 2:   // HTCAPTION
                    if (_rootPage?.ViewModel.ToggleWindowPinnedCommand.CanExecute(null) ?? false)
                    {
                        _rootPage?.ViewModel.ToggleWindowPinnedCommand.Execute(null);
                    }
                    args.Handled = true;
                    break;
                case NativeHelper.WM_NCRBUTTONUP when args.Message.WParam == 2:   // HTCAPTION
                    int x = (short)(args.Message.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((args.Message.LParam.ToInt32() >> 16) & 0xFFFF);
                    var point = new PointInt32(x, y);
                    NativeHelper.ScreenToClient(Handle, ref point);
                    float scale = (float)GetDpiScaleFactor();
                    TitleBarContextMenu?.ShowAt(_rootPage, new(point.X / scale, point.Y / scale));
                    break;
                case NativeHelper.WM_QUERYENDSESSION:
                    if (args.Message.LParam == 1)   // ENDSESSION_CLOSEAPP
                    {
                        NativeHelper.RegisterApplicationRestart(string.Empty, 0x1011);   // RESTART_NO_CRASH  | RESTART_NO_HANG  | RESTART_NO_REBOOT
                    }
                    args.Result = 1;
                    args.Handled = true;
                    break;

                case NativeHelper.WM_ENDSESSION when args.Message.WParam != 0:   // wParam = 1 means the session is ending
                    App.Current.Exit();
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

        private void GlobalMouseHook_MouseEvent(object? sender, MouseHookEventArgs e)
        {
            // Only handle left button down to detect clicks
            if (e.Message != GlobalMouseHook.MouseMessage.WM_LBUTTONDOWN && e.Message != GlobalMouseHook.MouseMessage.WM_RBUTTONDOWN)
            {
                return;
            }

            if (!Visible || Pinned)
            {
                return;
            }

            var windowBounds = new RectInt32(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);

            if (!IsPointInRect(e.Position, windowBounds))
            {
                DispatcherQueue.TryEnqueue(() => HideWindow());
            }
        }

        private bool IsPointInRect(PointInt32 point, RectInt32 rect)
        {
            return point.X >= rect.X && point.X < rect.X + rect.Width && point.Y >= rect.Y && point.Y < rect.Y + rect.Height;
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
            _globalMouseHook.MouseEvent -= GlobalMouseHook_MouseEvent;
            _globalMouseHook.Dispose();
            Activated -= Window_Activated;
            AppWindow.Closing -= Window_Closing;
            Closed -= Window_Closed;
            _messageMonitor.WindowMessageReceived -= WindowMessageReceived;
            _rootPage?.ActualThemeChanged -= ClipboardWindow_ActualThemeChanged;
            _rootPage?.WindowCaptionArea.SizeChanged -= WindowCaptionArea_SizeChanged;
            _rootPage?.ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
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

        #region TrayIcon

        private void TrayIcon_ContextMenu(TrayIcon sender, TrayIconEventArgs args)
        {
            if (TrayIconMenu is null)
            {
                TrayIconMenu = (MenuFlyout)App.Current.Resources["TrayIconContextMenu"];
                foreach (var item in TrayIconMenu.Items)
                {
                    item.DataContext = _rootPage?.ViewModel;
                }
            }

            args.Flyout = TrayIconMenu;
        }

        private async void TrayIcon_Click(TrayIcon sender, TrayIconEventArgs args)
        {
            // If we already have a pending single click, ignore extra Selected events
            if (_trayIconSingleClickPending)
            {
                return;
            }

            _trayIconSingleClickPending = true;

            await Task.Delay(TrayIconDoubleClickDelay);

            if (!_trayIconDoubleClickDetected)
            {
                ShowWindow(ClipboardWindowPosition.RightCorner);
            }

            await Task.Delay(TrayIconDoubleClickDelay);

            // Reset for next cycle
            _trayIconDoubleClickDetected = false;
            _trayIconSingleClickPending = false;
        }

        private void TrayIcon_LeftDoubleClick(TrayIcon sender, TrayIconEventArgs args)
        {
            _trayIconDoubleClickDetected = true;

            if (_rootPage?.ViewModel.ToggleClipboardMonitoringEnabledCommand.CanExecute(null) ?? false)
            {
                _rootPage.ViewModel.ToggleClipboardMonitoringEnabledCommand.Execute(null);
            }
        }

        private TrayIcon CreateTrayIcon()
        {
            var (iconPath, tooltip) = GetTrayIconAndTooltip();
            var trayIcon = new TrayIcon(TrayIconId, iconPath, tooltip);
            trayIcon.ContextMenu += TrayIcon_ContextMenu;
            trayIcon.Selected += TrayIcon_Click;
            trayIcon.LeftDoubleClick += TrayIcon_LeftDoubleClick;
            return trayIcon;
        }

        private (string, string) GetTrayIconAndTooltip()
        {
            var isMonitoringEnabled = _rootPage?.ViewModel.IsClipboardMonitoringEnabled ?? SettingsContext.IsClipboardMonitoringEnabled;
#if DEBUG
            string tooltip = $"{"AppDescription".GetLocalizedResource()} (Dev)";
#else
            string tooltip = "AppDescription".GetLocalizedResource();
#endif
            var trayIconPath = AppContext.BaseDirectory + (isMonitoringEnabled ? "Assets\\WindowIcon.ico" : "Assets\\WindowIcon.disabled.ico");

            if (!isMonitoringEnabled)
            {
                tooltip += Environment.NewLine + "/Clipboard/PauseMonitoringBanner/Text".GetLocalizedResource();
            }

            return (trayIconPath, tooltip);
        }

        #endregion

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
            var workArea = NativeHelper.GetWorkAreaFromPoint(out var dpiX, out var dpiY);   // Monitor DPI under the cursor pointer
            double dpiScaleX = dpiX / 96.0;   // 96 is a default DPI (scale 100%)
            double dpiScaleY = dpiY / 96.0;

            int windowWidth = SettingsContext.WindowWidth;
            int windowHeight = SettingsContext.WindowHeight;
            int windowMargin = SettingsContext.WindowMargin;

            int independedWidth = (int)(windowWidth * dpiScaleX);
            int independedHeight = (int)(windowHeight * dpiScaleY);
            int independedMarginX = (int)(windowMargin * dpiScaleX);
            int independedMarginY = (int)(windowMargin * dpiScaleY);

            // RightCorner as fallback position
            int fallbackPositionX = workArea.X + workArea.Width - independedWidth - independedMarginX;
            int fallbackPositionY = workArea.Y + workArea.Height - independedHeight - independedMarginY;

            //this.AppWindow.MoveAndResize - requires restart after DPI update, width and height depends on DPI
            //this.MoveAndResize - don't require restart after DPI update

            switch (position ?? SettingsContext.WindowPosition)
            {
                case ClipboardWindowPosition.Caret:
                    var caretPosition = GetPositionWindowRelativeToCaret(fallbackPositionX, fallbackPositionY);
                    MoveAndResize(caretPosition.X, caretPosition.Y, windowWidth, windowHeight);
                    break;
                case ClipboardWindowPosition.Cursor:
                    NativeHelper.GetCursorPos(out var cursorPos);
                    var newCursorPositionPos = AdjustWindowPositionToWorkArea(cursorPos, new(independedWidth, independedHeight), workArea);
                    MoveAndResize(newCursorPositionPos.X, newCursorPositionPos.Y, windowWidth, windowHeight);
                    break;
                case ClipboardWindowPosition.ScreenCenter:
                    MoveAndResize(
                        workArea.X + (workArea.Width - independedWidth) / 2,
                        workArea.Y + (workArea.Height - independedHeight) / 2,
                        windowWidth,
                        windowHeight);
                    break;
                case ClipboardWindowPosition.LastPosition:
                    MoveAndResize(AppWindow.Position.X, AppWindow.Position.Y, windowWidth, windowHeight);
                    break;
                case ClipboardWindowPosition.Right:
                    MoveAndResize(
                        fallbackPositionX,
                        workArea.Y + independedMarginY,
                        windowWidth,
                        (int)((workArea.Height - 2 * independedMarginY) / dpiScaleY));
                    break;
                case ClipboardWindowPosition.RightCorner:
                    MoveAndResize(fallbackPositionX, fallbackPositionY, windowWidth, windowHeight);
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

        private PointInt32 GetPositionWindowRelativeToCaret(int fallbackPositionX, int fallbackPositionY)
        {
            int x = fallbackPositionX;
            int y = fallbackPositionY;

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

        /// <summary>
        /// Move window to new position and resize if required
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width before DPI scale</param>
        /// <param name="height">Height before DPI scale</param>
        private void MoveAndResize(int x, int y, int width, int height)
        {
            AppWindow.Move(new(x,y));

            var windowDpiScale = GetDpiScaleFactor();
            int independedWidth = (int)(width * windowDpiScale);
            int independedHeight = (int)(height * windowDpiScale);

            // The AppWindow.Size is automatically calculated based on the device's DPI scale,
            // but this may not be accurate when there are changes in DPI or when the application is run on displays with varying DPI values.
            // It is recommended to manually adjust the window size after relocating it to the desired position.
            if (AppWindow.Size.Width != independedWidth || AppWindow.Size.Height != independedHeight)
            {
                AppWindow.MoveAndResize(new(x, y, independedWidth, independedHeight));
            }
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
