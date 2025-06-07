using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rememory.Helper;
using System;
using Windows.ApplicationModel;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;

namespace Rememory.Views.Settings
{
    public class SettingsWindow
    {
        public static event EventHandler<WindowActivatedEventArgs>? WindowActivated;
        public static IntPtr WindowHandle => _window is not null ? WindowNative.GetWindowHandle(_window) : IntPtr.Zero;

        private static Window? _window;

        private SettingsWindow() { }

        public static void ShowSettingsWindow()
        {
            _window ??= InitializeWindow();
            _window.Activate();
        }

        public static void CloseSettingsWindow()
        {
            if (_window is not null)
            {
                _window.Activated -= Window_Activated;
                _window.Close();
            }
        }

        private static Window InitializeWindow()
        {
            Window window = new WindowEx()
            {
                MinHeight = 500,
                MinWidth = 500,
                ExtendsContentIntoTitleBar = true,
                SystemBackdrop = new MicaBackdrop()
            };
            window.Content = new SettingsRootPage(window);
            window.Closed += SettingsWindow_Closed;

            window.AppWindow.Title = "SettingsWindow_Title".GetLocalizedFormatResource(AppInfo.Current.DisplayInfo.DisplayName);
            window.AppWindow.SetIcon("Assets\\WindowIcon.ico");
            window.AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, window.AppWindow.ClientSize.Width, 48)]);

            window.Activated += Window_Activated;

            return window;
        }

        private static void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            WindowActivated?.Invoke(sender, args);
        }

        private static void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_window is not null)
            {
                _window.Closed -= SettingsWindow_Closed;
                _window = null;
            }
        }
    }
}
