using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Helper;
using System;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;

namespace Rememory.Views.Settings
{
    public class SettingsWindow
    {
        public static event TypedEventHandler<object, WindowActivatedEventArgs>? WindowActivated;
        public static IntPtr WindowHandle => _window is not null ? WindowNative.GetWindowHandle(_window) : IntPtr.Zero;
        public static WindowId WindowId => _window?.AppWindow.Id ?? new(0);

        private static Window? _window;

        private SettingsWindow() { }

        public static void ShowSettingsWindow()
        {
            if (_window is null)
            {
                InitializeWindow();
            }
            _window?.Activate();
        }

        public static void CloseSettingsWindow() => _window?.Close();

        private static void InitializeWindow()
        {
            var windowTitle = "/Settings/WindowTitle/Text".GetLocalizedFormatResource(AppInfo.Current.DisplayInfo.DisplayName);

            _window = new WindowEx
            {
                MinHeight = 500,
                MinWidth = 500,
                ExtendsContentIntoTitleBar = true,
                SystemBackdrop = new MicaBackdrop()
            };

            _window.Content = new SettingsRootPage(_window) { Title = windowTitle };
            _window.AppWindow.Title = windowTitle;
            _window.AppWindow.SetIcon("Assets\\WindowIcon.ico");
            _window.AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, _window.AppWindow.ClientSize.Width, 48)]);

            _window.Activated += Window_Activated;
            _window.Closed += SettingsWindow_Closed;
            App.Current.ThemeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        private static void Window_Activated(object sender, WindowActivatedEventArgs args) => WindowActivated?.Invoke(sender, args);

        private static void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            App.Current.ThemeService.ThemeChanged -= ThemeService_ThemeChanged;

            if (_window is null)
            {
                return;
            }

            _window.Activated -= Window_Activated;
            _window.Closed -= SettingsWindow_Closed;
            _window.Content = null;
            _window = null;
        }

        private static void ThemeService_ThemeChanged(IThemeService sender, ElementTheme e) => _window?.AppWindow.TitleBar.PreferredTheme = (TitleBarTheme)(sender.Theme + 1);
    }
}
