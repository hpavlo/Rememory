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
        public static event EventHandler<WindowActivatedEventArgs> WindowActivated;
        public static IntPtr WindowHandle => _window is not null ? WindowNative.GetWindowHandle(_window) : IntPtr.Zero;

        private static Window _window;

        private SettingsWindow() { }

        public static void ShowSettingsWindow()
        {
            if (_window is null)
            {
                InitializeWindow();
                _window.Activate();
            }
            else
            {
                _window.Activate();
            }
        }

        public static void CloseSettingsWindow()
        {
            if (_window is not null)
            {
                _window.Activated -= Window_Activated;
                _window.Close();
            }
        }

        private static void InitializeWindow()
        {
            _window = new WindowEx()
            {
                MinHeight = 500,
                MinWidth = 500,
                ExtendsContentIntoTitleBar = true,
                SystemBackdrop = new MicaBackdrop()
            };
            _window.Content = new SettingsRootPage(_window);
            _window.Closed += SettingsWindow_Closed;

            _window.AppWindow.Title = $"{"AppDisplayName".GetLocalizedResource()} Settings";
            _window.AppWindow.SetIcon("Assets\\WindowIcon.ico");
            _window.AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, _window.AppWindow.ClientSize.Width, 48)]);

            _window.Activated += Window_Activated;
        }

        private static void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            WindowActivated?.Invoke(sender, args);
        }

        private static void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            _window = null;
        }
    }
}
