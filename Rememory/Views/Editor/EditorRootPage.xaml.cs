using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Service;
using Rememory.ViewModels;
using System;

namespace Rememory.Views.Editor
{
    public sealed partial class EditorRootPage : Page
    {
        public readonly EditorRootPageViewModel ViewModel;

        private IThemeService ThemeService => App.Current.ThemeService;
        private readonly Window _window;

        // If youser press button to close window
        private bool _requestToClose;

        public EditorRootPage(Window window, ClipboardItem context)
        {
            _window = window;
            ViewModel = new EditorRootPageViewModel(context);
            this.InitializeComponent();

            _window.SetTitleBar(WindowTitleBar);
            _window.AppWindow.Closing += EditorWindow_Closing;

            ApplyTheme();
            ThemeService.ThemeChanged += (s, a) => ApplyTheme();
        }

        private void ApplyTheme()
        {
            RequestedTheme = ThemeService.Theme;
        }

        private async void EditorWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (ViewModel.IsTextChanged)
            {
                args.Cancel = true;
                if (!_requestToClose)
                {
                    _requestToClose = true;
                    var dialog = new ContentDialog
                    {
                        Title = _window.AppWindow.Title,
                        Content = new TextBlock() { Text = "EditorDialogBox_Content".GetLocalizedResource() },
                        PrimaryButtonText = "Save".GetLocalizedResource(),
                        SecondaryButtonText = "DoNotSave".GetLocalizedResource(),
                        CloseButtonText = "Cancel".GetLocalizedResource(),
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        ViewModel.SaveTextCommand.Execute(null);
                    }
                    if (result != ContentDialogResult.None)
                    {
                        _window.Close();
                    }
                    _requestToClose = false;
                }
            }
        }

        private void EditorTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            CharactersCountTextBlock.Text = "EditorWindowFooter_CharactersCount".GetLocalizedFormatResource(EditorTextBox.Text.Length);
        }

        // CanUndo and CanRedo doesn't work with Binding
        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CanUndoButton.IsEnabled = EditorTextBox.CanUndo;
            CanRedoButton.IsEnabled = EditorTextBox.CanRedo;
        }

        private void EditorTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            CharactersCountTextBlock.Text = EditorTextBox.SelectionLength > 0
                    ? "EditorWindowFooter_SelectedCharactersCount".GetLocalizedFormatResource(EditorTextBox.SelectionLength, EditorTextBox.Text.Length)
                    : "EditorWindowFooter_CharactersCount".GetLocalizedFormatResource(EditorTextBox.Text.Length);
        }

        private unsafe void PresenterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (_window.AppWindow.Presenter is CompactOverlayPresenter)
            {
                _window.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                ((FontIcon)button.Content).Glyph = "\uE73F";
                ToolTipService.SetToolTip(button, "Editor_CompactButton/ToolTipService/ToolTip".GetLocalizedResource());
            }
            else
            {
                _window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
                ((FontIcon)button.Content).Glyph = "\uE740";
                ToolTipService.SetToolTip(button, "Editor_ExtendButton/ToolTipService/ToolTip".GetLocalizedResource());
            }
        }

        private void EditorTextBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(EditorTextBox);
            if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control && !point.Properties.IsHorizontalMouseWheel)
            {
                if (point.Properties.MouseWheelDelta > 0)
                {
                    UpFontSize();
                }
                else
                {
                    DownFontSize();
                }
            }
        }

        private void UpFontSize()
        {
            if (EditorTextBox.FontSize <= 68)
            {
                EditorTextBox.FontSize += 2;
                ScaleTextBlock.Text = Math.Ceiling(EditorTextBox.FontSize / 14 * 100) + "%";
            }
        }

        private void DownFontSize()
        {
            if (EditorTextBox.FontSize >= 4)
            {
                EditorTextBox.FontSize -= 2;
                ScaleTextBlock.Text = Math.Ceiling(EditorTextBox.FontSize / 14 * 100) + "%";
            }
        }

        private void EordWrapToggle_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (ToggleMenuFlyoutItem)sender;
            EditorTextBox.TextWrapping = menuItem.IsChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }
    }
}
