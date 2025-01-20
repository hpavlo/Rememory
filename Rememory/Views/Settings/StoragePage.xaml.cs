using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.ViewModels;
using System;

namespace Rememory.Views.Settings
{
    public sealed partial class StoragePage : Page
    {
        public readonly SettingsStoragePageViewModel ViewModel = new();

        public StoragePage()
        {
            this.InitializeComponent();
        }

        private async void EraseClipboardDataButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = "DeleteAllSavedDataQuestion".GetLocalizedResource(),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                ViewModel.EraseClipboardDataCommand?.Execute(null);
            }
        }
    }
}
