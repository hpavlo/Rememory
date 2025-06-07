using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Rememory.Contracts;
using Rememory.Views;
using System;
using WinRT;

namespace Rememory.Helper.WindowBackdrop
{
    /// <summary>
    /// Manages system backdrop effects (Mica, Acrylic) for a target Window.
    /// Handles initialization, switching between backdrop types based on settings or themes,
    /// responding to window activation/theme changes, and resource cleanup.
    /// </summary>
    public class WindowBackdropHelper(ClipboardWindow window)
    {
        /// <summary>
        /// Gets a value indicating whether system backdrops (Mica and Acrylic) are supported on the current system.
        /// Checks if both controller types are supported by the OS version and hardware.
        /// </summary>
        public static bool IsSystemBackdropSupported => DesktopAcrylicController.IsSupported() && MicaController.IsSupported();

        private readonly ClipboardWindow _window = window;
        private WindowsSystemDispatcherQueueHelper? _wsdqHelper;
        private DesktopAcrylicController? _acrylicController;
        private MicaController? _micaController;
        private SystemBackdropConfiguration? _configurationSource;
        private IThemeService ThemeService => App.Current.ThemeService;

        /// <summary>
        /// Initializes the backdrop system for the window if supported.
        /// Sets up controllers, configuration, and event handlers.
        /// </summary>
        /// <returns><c>true</c> if system backdrops are supported and initialization was successful; otherwise, <c>false</c>.</returns>
        public bool TryInitializeBackdrop()
        {
            if (IsSystemBackdropSupported)
            {
                _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                _configurationSource = new SystemBackdropConfiguration();
                _window.Showing += Window_Showing;
                _window.Hiding += Window_Hiding;
                _window.Closed += Window_Closed;
                ((FrameworkElement)_window.Content).ActualThemeChanged += Window_ThemeChanged;
                _configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                SetWindowBackdrop(ThemeService.WindowBackdrop);
                ThemeService.WindowBackdropChanged += ThemeService_WindowBackdropChanged;

                return true;
            }
            return false;
        }

        private void SetWindowBackdrop(WindowBackdropType type)
        {
            if (!IsSystemBackdropSupported)
            {
                return;
            }

            switch (type)
            {
                case WindowBackdropType.None:
                    {
                        _micaController?.RemoveAllSystemBackdropTargets();
                        _acrylicController?.RemoveAllSystemBackdropTargets();
                        break;
                    }
                case WindowBackdropType.Acrylic:
                    {
                        SetAcrylicBackdrop(DesktopAcrylicKind.Base);
                        break;
                    }
                case WindowBackdropType.ThinAcrylic:
                    {
                        SetAcrylicBackdrop(DesktopAcrylicKind.Thin);
                        break;
                    }
                case WindowBackdropType.Mica:
                    {
                        SetMicaBackdrop(MicaKind.Base);
                        break;
                    }
                case WindowBackdropType.MicaAlt:
                    {
                        SetMicaBackdrop(MicaKind.BaseAlt);
                        break;
                    }
            }
        }

        private void ThemeService_WindowBackdropChanged(object? sender, WindowBackdropType e) => SetWindowBackdrop(e);

        private void SetAcrylicBackdrop(DesktopAcrylicKind kind)
        {
            _acrylicController ??= new();
            _acrylicController.Kind = kind;
            _acrylicController.SetSystemBackdropConfiguration(_configurationSource);

            _micaController?.RemoveAllSystemBackdropTargets();
            _acrylicController.AddSystemBackdropTarget(_window.As<ICompositionSupportsSystemBackdrop>());
        }

        private void SetMicaBackdrop(MicaKind kind)
        {
            _micaController ??= new();
            _micaController.Kind = kind;
            _micaController.SetSystemBackdropConfiguration(_configurationSource);

            _acrylicController?.RemoveAllSystemBackdropTargets();
            _micaController.AddSystemBackdropTarget(_window.As<ICompositionSupportsSystemBackdrop>());
        }

        private void Window_Showing(ClipboardWindow sender, EventArgs args)
        {
            if (_configurationSource is not null)
            {
                _configurationSource.IsInputActive = true;
            }
        }

        private void Window_Hiding(ClipboardWindow sender, EventArgs args)
        {
            if (_configurationSource is not null)
            {
                _configurationSource.IsInputActive = false;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed _window.
            if (_acrylicController is not null)
            {
                _acrylicController.Dispose();
                _acrylicController = null;
            }
            if (_micaController is not null)
            {
                _micaController.Dispose();
                _micaController = null;
            }
            _window.Showing -= Window_Showing;
            _window.Hiding -= Window_Hiding;
            _window.Closed -= Window_Closed;
            ((FrameworkElement)_window.Content).ActualThemeChanged -= Window_ThemeChanged;
            ThemeService.WindowBackdropChanged -= ThemeService_WindowBackdropChanged;
            _configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            SetConfigurationSourceTheme();
        }

        private void SetConfigurationSourceTheme()
        {
            if (_configurationSource is not null)
            {
                switch (((FrameworkElement)_window.Content).ActualTheme)
                {
                    case ElementTheme.Dark: _configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: _configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: _configurationSource.Theme = SystemBackdropTheme.Default; break;
                }
            }
            
        }
    }

    /// <summary>
    /// Defines the types of system backdrops supported.
    /// </summary>
    public enum WindowBackdropType
    {
        None,
        Acrylic,
        ThinAcrylic,
        Mica,
        MicaAlt
    }
}
