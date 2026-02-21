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

        private void EraseDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.EraseClipboardDataCommand.CanExecute(null))
            {
                ViewModel.EraseClipboardDataCommand.Execute(null);
            }
            EraseDataFlyout.Hide();
            EraseDataInfoBadge.Visibility = Visibility.Visible;
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
