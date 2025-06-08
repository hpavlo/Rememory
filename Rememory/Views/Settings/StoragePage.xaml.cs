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

        private readonly ContentDialog _ownerFilterEditorDialog;
        private readonly ContentDialog _tagEditorDialog;

        public StoragePage()
        {
            InitializeComponent();

            _ownerFilterEditorDialog = new()
            {
                Title = "FilterDialogBox_Title".GetLocalizedResource(),
                PrimaryButtonText = "Save".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            _tagEditorDialog = new()
            {
                Title = "TagDialogBox_Title".GetLocalizedResource(),
                PrimaryButtonText = "Save".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ownerFilterEditorDialog.XamlRoot = XamlRoot;
            _tagEditorDialog.XamlRoot = XamlRoot;
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

        private async void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            var dialogContent = new TagEditorDialog();
            _tagEditorDialog.RequestedTheme = App.Current.ThemeService.Theme;
            _tagEditorDialog.IsPrimaryButtonEnabled = false;
            _tagEditorDialog.Content = dialogContent;
            var dialogResult = await _tagEditorDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                ViewModel.AddTag(dialogContent.TagName, dialogContent.SelectedColor);
            }
        }

        private async void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (TagModel)((Button)sender).DataContext;
            var dialogContent = new TagEditorDialog()
            {
                TagName = tag.Name,
                SelectedColor = tag.ColorBrush
            };
            _tagEditorDialog.RequestedTheme = App.Current.ThemeService.Theme;
            _tagEditorDialog.Content = dialogContent;
            var dialogResult = await _tagEditorDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                ViewModel.EditTag(tag, dialogContent.TagName, dialogContent.SelectedColor);
            }
        }
    }
}
