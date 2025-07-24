using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Globalization;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Hooks;
using Rememory.Models;
using Rememory.Services;
using Rememory.Views;
using Rememory.Views.Settings;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using WinRT.Interop;

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
        public IntPtr ClipboardWindowHandle { get; private set; }
        public ClipboardWindow ClipboardWindow { get; private set; }
        public IThemeService ThemeService { get; private set; }
        public SettingsContext SettingsContext => SettingsContext.Instance;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        private string[] _launchArguments;
        private IKeyboardMonitor _keyboardMonitor;
        private bool _closeApp = false;

        /// <summary>
        /// Initializes the singleton application object.
        /// </summary>
        public App(string[] args)
        {
            _launchArguments = args;
            Services = ConfigureServices();
            ThemeService = Services.GetService<IThemeService>()!;
            _keyboardMonitor = Services.GetService<IKeyboardMonitor>()!;

            InitializeComponent();
            SetCulture(SettingsContext.LanguageCode.Equals(string.Empty) ?
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName : SettingsContext.LanguageCode);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            ClipboardWindow = new ClipboardWindow();
            ClipboardWindow.Closed += ClipboardWindow_Closed;
            ClipboardWindowHandle = WindowNative.GetWindowHandle(ClipboardWindow);

            var rootPage = new ClipboardRootPage(ClipboardWindow);
            // Return if we closed app during ClipboardRootPage initializing
            if (_closeApp) return;

            ClipboardWindow.Content = rootPage;
            ClipboardWindow.InitSystemBackdrop();
            ClipboardWindow.InitSystemThemeTrigger();

            _keyboardMonitor.StartMonitor();

            if (_launchArguments.Contains("-settings"))
            {
                SettingsWindow.ShowSettingsWindow();
            }

            if (!_launchArguments.Contains("-silent") && SettingsContext.IsNotificationOnStartEnabled)
            {
                AppNotificationManager.Default.Show(new AppNotificationBuilder()
                    .AddText("AppNotification_AppIsRunning".GetLocalizedResource())
                    .AddText("AppNotification_UseShortcutToOpen".GetLocalizedFormatResource(
                        KeyboardHelper.ShortcutToString(SettingsContext.ActivationShortcut, "+")))
                    .BuildNotification());
            }
        }

        private void ClipboardWindow_Closed(object sender, WindowEventArgs args)
        {
            _closeApp = true;
            ClipboardWindow.Closed -= ClipboardWindow_Closed;
            _keyboardMonitor.StopMonitor();
            Exit();
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
            services.AddSingleton<IOwnerService, OwnerService>();
            services.AddSingleton<ITagService, TagService>();
            services.AddTransient<IStartupService, TaskSchedulerStartupService>();

            return services.BuildServiceProvider();
        }
    }
}
