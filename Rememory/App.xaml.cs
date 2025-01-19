﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Globalization;
using Rememory.Helper;
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
using WinRT.Interop;

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
        public IntPtr ClipboardWindowHandle { get; private set; }
        public IThemeService ThemeService { get; private set; }
        public SettingsContext SettingsContext => SettingsContext.Instance;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        private ClipboardWindow _clipboardWindow;
        private string[] _launchArguments;
        private IKeyboardMonitor _keyboardMonitor;

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
            _clipboardWindow = new ClipboardWindow();
            _clipboardWindow.Closed += ClipboardWindow_Closed;
            ClipboardWindowHandle = WindowNative.GetWindowHandle(_clipboardWindow);

            InitializeRememoryCore();
            _keyboardMonitor.StartMonitor();

            if (_launchArguments.Contains("-settings"))
            {
                SettingsWindow.ShowSettingsWindow();
            }

            if (!_launchArguments.Contains("-silent"))
            {
                AppNotificationManager.Default.Show(new AppNotificationBuilder()
                    .AddText("AppNotification_AppIsRunning".GetLocalizedResource())
                    .AddText("AppNotification_UseShortcutToOpen".GetLocalizedFormatResource(
                        KeyboardHelper.ShortcutToString(SettingsContext.ActivationShortcut, "+")))
                    .BuildNotification());
            }
        }

        public void ShowClipboardWindow() => _clipboardWindow.ShowWindow();
        public void HideClipboardWindow() => _clipboardWindow.HideWindow();

        private void ClipboardWindow_Closed(object sender, WindowEventArgs args)
        {
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

        private unsafe void InitializeRememoryCore()
        {
            RememoryCoreHelper.AddWindowProc(ClipboardWindowHandle);
            RememoryCoreHelper.CreateTrayIcon(ClipboardWindowHandle,
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
