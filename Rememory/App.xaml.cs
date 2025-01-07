using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Globalization;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Hooks;
using Rememory.Models;
using Rememory.Service;
using Rememory.Views;
using Rememory.Views.Settings;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;
        public IntPtr ClipboardWindowHwnd { get; private set; }
        public Window ClipboardWindow { get; private set; }
        public IThemeService ThemeService { get; private set; }
        public SettingsContext SettingsContext => SettingsContext.Instance;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        private string[] _launchArguments;
        private IKeyboardMonitor _keyboardMonitor;
        private WindowMessageMonitor _messageMonitor;
        private bool _queryEndSessionReceived = false;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App(string[] args)
        {
            _launchArguments = args;
            Services = ConfigureServices();
            ThemeService = Services.GetService<IThemeService>();
            _keyboardMonitor = Services.GetService<IKeyboardMonitor>();

            this.InitializeComponent();
            SetCulture(SettingsContext.CurrentLanguageCode.Equals(string.Empty) ?
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName : SettingsContext.CurrentLanguageCode);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            InitializeMainWindow();

            if (_launchArguments.Contains("-settings"))
            {
                SettingsWindow.ShowSettingsWindow();
            }

            InitializeRememoryCore();

            _messageMonitor = new WindowMessageMonitor(ClipboardWindowHwnd);
            _messageMonitor.WindowMessageReceived += WindowMessageReceived;
            _keyboardMonitor.StartMonitor();

            if (!_launchArguments.Contains("-silent"))
            {
                AppNotificationManager.Default.Show(new AppNotificationBuilder()
                    .AddText("AppNotification_AppIsRunning".GetLocalizedResource())
                    .AddText("AppNotification_UseShortcutToOpen".GetLocalizedFormatResource(
                        KeyboardHelper.ShortcutToString(SettingsContext.ActivationShortcut, "+")))
                    .BuildNotification());
            }
        }

        private void InitializeMainWindow()
        {
            ClipboardWindow = new WindowEx
            {
                Title = Package.Current.DisplayName,
                IsShownInSwitchers = false,
                IsAlwaysOnTop = true,
                IsResizable = false,
                IsMaximizable = false,
                IsMinimizable = false,
                TaskBarIcon = Icon.FromFile(AppContext.BaseDirectory + "Assets\\WindowIcon.ico")
            };
            ClipboardWindow.SetWindowStyle(WindowStyle.Popup);

            ClipboardWindowHwnd = WindowNative.GetWindowHandle(ClipboardWindow);
            int cornerPreference = (int)NativeHelper.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            NativeHelper.DwmSetWindowAttribute(ClipboardWindowHwnd, NativeHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            ClipboardWindow.Content = new ClipboardRootPage(ClipboardWindow);

            if (WindowBackdropHelper.IsSystemBackdropSupported)
            {
                var backdropHelper = new WindowBackdropHelper(ClipboardWindow);
                backdropHelper.InitWindowBackdrop();
            }

            ClipboardWindow.Activated += ClipboardWindow_Activated;
            ClipboardWindow.AppWindow.Closing += ClipboardWindow_Closing;
            ClipboardWindow.Closed += ClipboardWindow_Closed;
        }

        private void SetCulture(string culture)
        {
            var cultureInfo = new CultureInfo(culture);

            // Set the culture for the current thread
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            // Ensure new threads will use this culture
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            ApplicationLanguages.PrimaryLanguageOverride = culture;
        }

        private unsafe void InitializeRememoryCore()
        {
            RememoryCoreHelper.AddWindowProc(ClipboardWindowHwnd);
            RememoryCoreHelper.CreateTrayIcon(ClipboardWindowHwnd,
                new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged(
                    $"{"TrayIconMenu_Open".GetLocalizedResource()}\t{KeyboardHelper.ShortcutToString(SettingsContext.ActivationShortcut, "+")}")),
                new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged("TrayIconMenu_Settings".GetLocalizedResource())),
                new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged("TrayIconMenu_Exit".GetLocalizedResource())),
#if DEBUG
                new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged($"{"AppDescription".GetLocalizedResource()} (Dev)"))
#else
                new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged("AppDescription".GetLocalizedResource()))
#endif
                );
        }

        public bool ShowClipboardWindow()
        {
            if (ClipboardWindow.Visible == true)
                return false;
            MoveToStartPosition();
            ClipboardWindow.AppWindow.Show();
            KeyboardHelper.MultiKeyAction([(VirtualKey)0x0E], KeyboardHelper.KeyAction.DownUp);   // To fix problem with foreground window
            ClipboardWindow.SetForegroundWindow();
            return true;
        }

        public bool HideClipboardWindow()
        {
            if (!ClipboardWindow.Visible)
            {
                return false;
            }
            ClipboardWindow.AppWindow.Hide();
            return true;
        }

        public void MoveToStartPosition()
        {
            var workArea = GetWorkAreaRectangle();

            int width = SettingsContext.WindowWidth;
            int margin = SettingsContext.WindowMargin;

            // To update DPI for window
            ClipboardWindow.AppWindow.Move(new(
                (int)workArea.Right - width - margin,
                (int)workArea.Top + margin));

            // Resize window
            ClipboardWindow.AppWindow.MoveAndResize(new RectInt32(
                (int)workArea.Right - width - margin,
                (int)workArea.Top + margin,
                width,
                (int)workArea.Height - 2 * margin));
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
                            ShowClipboardWindow();
                            break;
                        case RememoryCoreHelper.TRAY_SETTINGS_COMMAND:
                            SettingsWindow.ShowSettingsWindow();
                            break;
                        case RememoryCoreHelper.TRAY_EXIT_COMMAND:
                            ClipboardWindow.Close();
                            break;
                    }
                    break;
                case RememoryCoreHelper.TRAY_NOTIFICATION:
                    if (args.Message.LParam == NativeHelper.WM_LBUTTONDBLCLK)
                        SettingsWindow.ShowSettingsWindow();
                    break;
            }
        }

        private void ClipboardWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                HideClipboardWindow();
            }
        }

        private void ClipboardWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (!_queryEndSessionReceived)
            {
                HideClipboardWindow();
                args.Cancel = true;
            }
        }

        private void ClipboardWindow_Closed(object sender, WindowEventArgs args)
        {
            SettingsWindow.CloseSettingsWindow();
            _keyboardMonitor.StopMonitor();
            ClipboardWindow.Activated -= ClipboardWindow_Activated;
            _messageMonitor.WindowMessageReceived -= WindowMessageReceived;
            Exit();
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
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IStorageService, SqliteService>();
            services.AddSingleton<ISearchService, SearchService>();
            services.AddSingleton<ILinkPreviewService, LinkPreviewService>();
            services.AddSingleton<ICleanupDataService, CleanupDataService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IKeyboardMonitor, KeyboardMonitor>();
            services.AddTransient<IStartupService, TaskSchedulerStartupService>();

            return services.BuildServiceProvider();
        }
    }
}
