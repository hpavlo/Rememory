using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Rememory.ViewModels.Settings;
using System;

namespace Rememory.Views.Settings
{
    public sealed partial class GeneralPage : Page
    {
        public readonly GeneralPageViewModel ViewModel = new();

        public GeneralPage()
        {
            InitializeComponent();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                LanguageTeachingTip.IsOpen = true;
            }
        }

        private async void OpenSettingsStartupButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var uri = new Uri("ms-settings:startupapps");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Bindings.StopTracking();
            DataContext = null;
        }
    }
}
