using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.ViewModels;

namespace Rememory.Views.Settings
{
    public sealed partial class GeneralPage : Page
    {
        public readonly SettingsGeneralPageViewModel ViewModel = new();

        public GeneralPage()
        {
            this.InitializeComponent();
        }

        private void RestartAsAdministratorSettings_Loaded(object sender, RoutedEventArgs e)
        {
            var settingsExpander = (SettingsExpander)sender;
            settingsExpander.Header = (AdministratorHelper.IsAppRunningAsAdministrator() ?
                "SettingsExpanderHeader_RunningAsAdministrator" :
                "SettingsExpanderHeader_RestartAsAdministrator")
                .GetLocalizedResource();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                LanguageTeachingTip.IsOpen = true;
            }
        }
    }
}
