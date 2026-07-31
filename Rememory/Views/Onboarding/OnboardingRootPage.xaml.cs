using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Contracts;
using Rememory.ViewModels;
using Rememory.Views.Clipboard;

namespace Rememory.Views.Onboarding
{
    public sealed partial class OnboardingRootPage : Page
    {
        public readonly OnboardingRootPageViewModel ViewModel = new();

        private readonly IThemeService _themeService = App.Current.ThemeService;
        private Window? _parentWindow;

        public OnboardingRootPage(Window parentWindow)
        {
            _parentWindow = parentWindow;
            InitializeComponent();

            RequestedTheme = _themeService.Theme;
            _themeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        private void ThemeService_ThemeChanged(IThemeService sender, ElementTheme e) => RequestedTheme = _themeService.Theme;

        private async void Page_Loaded(object sender, RoutedEventArgs e) => await ViewModel.InitializeAsync();

        private void GetStartedButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow?.Close();
            _parentWindow = null;
            App.Current.ClipboardWindow.ShowWindow(ClipboardWindowPosition.ScreenCenter);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Bindings.StopTracking();
            DataContext = null;
            _parentWindow = null;
        }
    }
}
