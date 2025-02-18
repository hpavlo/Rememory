using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.ViewModels;

namespace Rememory.Views.Settings
{
    public sealed partial class StoragePage : Page
    {
        public readonly SettingsStoragePageViewModel ViewModel = new();

        public StoragePage()
        {
            this.InitializeComponent();
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
    }
}
