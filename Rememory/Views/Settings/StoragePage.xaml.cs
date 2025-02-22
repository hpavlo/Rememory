using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Models;
using Rememory.ViewModels;
using Rememory.Views.Settings.Controls;
using System;

namespace Rememory.Views.Settings
{
    public sealed partial class StoragePage : Page
    {
        public readonly SettingsStoragePageViewModel ViewModel = new();

        private ContentDialog _ownerAppFilterDialog;

        public StoragePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ownerAppFilterDialog = new()
            {
                Title = "FilterDialogBox_Title".GetLocalizedResource(),
                PrimaryButtonText = "Save".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
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

        private async void AddOwnerAppFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var dialogContent = new FilterEditorDialogContent(_ownerAppFilterDialog);
            _ownerAppFilterDialog.IsPrimaryButtonEnabled = false;
            _ownerAppFilterDialog.Content = dialogContent;
            var dialogResult = await _ownerAppFilterDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                ViewModel.AddOwnerAppFilter(dialogContent.FilterName, dialogContent.FilterPattern);
            }
        }

        private async void EditOwnerAppFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var filter = (OwnerAppFilter)((Button)sender).DataContext;
            var dialogContent = new FilterEditorDialogContent(_ownerAppFilterDialog)
            {
                FilterName = filter.Name,
                FilterPattern = filter.Pattern
            };
            _ownerAppFilterDialog.Content = dialogContent;
            var dialogResult = await _ownerAppFilterDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                ViewModel.EditOwnerAppFilter(filter, dialogContent.FilterName, dialogContent.FilterPattern);
            }
        }
    }
}
