using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Models;
using Rememory.ViewModels.Settings;
using Rememory.Views.Settings.Controls;
using System;

namespace Rememory.Views.Settings
{
    public sealed partial class FiltersPage : Page
    {
        public readonly FiltersPageViewModel ViewModel = new();

        private readonly ContentDialog _ownerFilterEditorDialog;

        public FiltersPage()
        {
            InitializeComponent();

            _ownerFilterEditorDialog = new()
            {
                Title = "/Settings/FilterDialog_Title/Text".GetLocalizedResource(),
                PrimaryButtonText = "Save".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ownerFilterEditorDialog.XamlRoot = XamlRoot;
        }

        private async void AddOwnerAppFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var dialogContent = new FilterEditorDialog();
            _ownerFilterEditorDialog.RequestedTheme = App.Current.ThemeService.Theme;
            _ownerFilterEditorDialog.IsPrimaryButtonEnabled = false;
            _ownerFilterEditorDialog.Content = dialogContent;
            var dialogResult = await _ownerFilterEditorDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                ViewModel.AddOwnerAppFilter(dialogContent.FilterName, dialogContent.FilterPattern);
            }
        }

        private async void EditOwnerAppFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var filter = (OwnerAppFilter)((Button)sender).DataContext;
            var dialogContent = new FilterEditorDialog()
            {
                FilterName = filter.Name,
                FilterPattern = filter.Pattern
            };
            _ownerFilterEditorDialog.RequestedTheme = App.Current.ThemeService.Theme;
            _ownerFilterEditorDialog.Content = dialogContent;
            var dialogResult = await _ownerFilterEditorDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                ViewModel.EditOwnerAppFilter(filter, dialogContent.FilterName, dialogContent.FilterPattern);
            }
        }
    }
}
