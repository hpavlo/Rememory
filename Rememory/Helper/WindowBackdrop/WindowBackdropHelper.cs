using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Rememory.Service;
using WinRT;

namespace Rememory.Helper.WindowBackdrop
{
    public class WindowBackdropHelper
    {
        public static bool IsSystemBackdropSupported => DesktopAcrylicController.IsSupported() && MicaController.IsSupported();

        private Window _window;
        private WindowsSystemDispatcherQueueHelper _wsdqHelper;
        private DesktopAcrylicController _acrylicController;
        private MicaController _micaController;
        private SystemBackdropConfiguration _configurationSource;
        private IThemeService ThemeService => App.Current.ThemeService;

        public WindowBackdropHelper(Window window)
        {
            _window = window;
        }

        public bool InitWindowBackdrop()
        {
            if (IsSystemBackdropSupported)
            {
                _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                _configurationSource = new SystemBackdropConfiguration();
                _window.Activated += Window_Activated;
                _window.Closed += Window_Closed;
                ((FrameworkElement)_window.Content).ActualThemeChanged += Window_ThemeChanged;
                _configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                ThemeService.WindowBackdropChanged += (s, a) => SetWindowBackdrop(a);
                SetWindowBackdrop(ThemeService.WindowBackdrop);

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

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed _window.
            if (_acrylicController != null)
            {
                _acrylicController.Dispose();
                _acrylicController = null;
            }
            if (_micaController != null)
            {
                _micaController.Dispose();
                _micaController = null;
            }
            _window.Activated -= Window_Activated;
            _configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)_window.Content).ActualTheme)
            {
                case ElementTheme.Dark: _configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: _configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: _configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }
    }

    public enum WindowBackdropType
    {
        None,
        Acrylic,
        ThinAcrylic,
        Mica,
        MicaAlt
    }
}
