using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.ViewModels.Settings;

namespace Rememory.Views.Settings
{
    public sealed partial class StoragePage : Page
    {
        public readonly StoragePageViewModel ViewModel = new();

        public StoragePage()
        {
            InitializeComponent();
            TriggerImportWarningMessageVisibility();

            ViewModel.IsExportInProgress = false;
            ViewModel.IsImportInProgress = false;
            ViewModel.IsExportedSuccessfully = null;
            ViewModel.IsImportedSuccessfully = null;
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SettingsContext.SkipWarningMessageOnSettingsClipsErase)
            {
                if (ViewModel.EraseClipsCommand.CanExecute(null))
                {
                    ViewModel.EraseClipsCommand.Execute(null);
                    EraseDataInfoBadge.Visibility = Visibility.Visible;
                }
            }
            else
            {
                EraseDataFlyout.ShowAt(sender as FrameworkElement);
            }
        }

        private void EraseFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            EraseDataFlyout.Hide();

            if (ViewModel.EraseClipsCommand.CanExecute(null))
            {
                ViewModel.EraseClipsCommand.Execute(null);
                EraseDataInfoBadge.Visibility = Visibility.Visible;
            }
        }

        private void TriggerImportWarningMessageVisibility()
        {
            ImportClipsWarningInfoBar.IsOpen = ViewModel.SettingsContext.CleanupType != Services.CleanupType.RetentionPeriod
                || ViewModel.SettingsContext.CleanupTimeSpan != Services.CleanupTimeSpan.None;
        }

        private void CleanupTypeComboBox_SelectionChanged(object _, SelectionChangedEventArgs __) => TriggerImportWarningMessageVisibility();
        private void RetentionPeriodComboBox_SelectionChanged(object _, SelectionChangedEventArgs __) => TriggerImportWarningMessageVisibility();
    }
}
