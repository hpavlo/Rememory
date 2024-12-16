using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using WinRT;

namespace Rememory.Helper.WindowBackdrop
{
    public class WindowBackdropHelper(Window window)
    {
        private Window window = window;
        private WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        private DesktopAcrylicController m_AcrylicController;
        private SystemBackdropConfiguration m_configurationSource;

        public bool TrySetAcrylicBackdrop(DesktopAcrylicKind kind)
        {
            if (DesktopAcrylicController.IsSupported())   // Or Mica
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new SystemBackdropConfiguration();
                this.window.Activated += Window_Activated;
                this.window.Closed += Window_Closed;
                ((FrameworkElement)this.window.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_AcrylicController = new();
                m_AcrylicController.Kind = kind;

                // Enable the system backdrop.
                m_AcrylicController.AddSystemBackdropTarget(this.window.As<ICompositionSupportsSystemBackdrop>());
                m_AcrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // Succeeded.
            }

            return false; // Acrylic is not supported on this system.
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_AcrylicController != null)
            {
                m_AcrylicController.Dispose();
                m_AcrylicController = null;
            }
            window.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)window.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }
    }
}
