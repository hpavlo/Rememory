using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.ViewModels.Settings;

namespace Rememory.Views.Settings
{
    public sealed partial class GeneralPage : Page
    {
        public readonly GeneralPageViewModel ViewModel = new();

        public GeneralPage()
        {
            InitializeComponent();
        }

        private void RestartAsAdministratorSettings_Loaded(object sender, RoutedEventArgs e)
        {
            var settingsExpander = (SettingsExpander)sender;
            settingsExpander.Header = (AdministratorHelper.IsAppRunningAsAdministrator() ?
                "/Settings/General_RunningAsAdmin/Header" :
                "/Settings/General_RestartAsAdmin/Header")
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
