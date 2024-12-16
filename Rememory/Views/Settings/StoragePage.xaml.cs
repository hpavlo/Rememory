using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.ViewModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
