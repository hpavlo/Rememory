using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Helper;
using System;
using Windows.ApplicationModel;
using Windows.UI.WindowManagement;
using WinRT.Interop;

namespace Rememory.Views.Onboarding
{
    public sealed class OnboardingWindow : Window
    {
        private const int WindowWidth_ = 650;
        private const int WindowHeight_ = 740;

        public OnboardingWindow()
        {
            SystemBackdrop = new MicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            Title = Package.Current.DisplayName;
            Content = new OnboardingRootPage(this);

            var presenter = OverlappedPresenter.Create();
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            AppWindow.SetPresenter(presenter);

            var appIconPath = AppContext.BaseDirectory + "Assets\\WindowIcon.ico";
            AppWindow.SetTaskbarIcon(appIconPath);
            AppWindow.SetTitleBarIcon(appIconPath);

            Closed += OnboardingWindow_Closed;
            App.Current.ThemeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        public void ShowOnScreenCenter()
        {
            var handle = WindowNative.GetWindowHandle(this);
            var dpi = NativeHelper.GetDpiForWindow(handle) / 96.0;
            int scaledWidth = (int)(WindowWidth_ * dpi);
            int scaledHeight = (int)(WindowHeight_ * dpi);

            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
            var x = (displayArea.WorkArea.Width - scaledWidth) / 2;
            var y = (displayArea.WorkArea.Height - scaledHeight) / 2;

            AppWindow.MoveAndResize(new(x, y, scaledWidth, scaledHeight));
            AppWindow.Show();
            NativeHelper.SetForegroundWindow(handle);
        }

        private void OnboardingWindow_Closed(object sender, WindowEventArgs args)
        {
            App.Current.ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
            Closed -= OnboardingWindow_Closed;
            Content = null;
        }

        private void ThemeService_ThemeChanged(IThemeService sender, ElementTheme e) => AppWindow.TitleBar.PreferredTheme = (TitleBarTheme)(sender.Theme + 1);
    }
}
