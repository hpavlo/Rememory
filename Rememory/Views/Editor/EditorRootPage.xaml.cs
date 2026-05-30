using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.ViewModels;
using System;

namespace Rememory.Views.Editor
{
    public sealed partial class EditorRootPage : Page
    {
        public readonly EditorRootPageViewModel ViewModel;

        private readonly IThemeService _themeService = App.Current.ThemeService;
        private Window? _window;

        // If user press button to close window
        private bool _requestToClose;

        public EditorRootPage(Window window, ClipModel context)
        {
            _window = window;
            ViewModel = new EditorRootPageViewModel(context);

            InitializeComponent();

            _window.SetTitleBar(WindowTitleBar);
            _window.AppWindow.Closing += EditorWindow_Closing;
            _window.Closed += EditorWindow_Closed;

            RequestedTheme = _themeService.Theme;
            _themeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        private void ThemeService_ThemeChanged(IThemeService sender, ElementTheme e) => RequestedTheme = _themeService.Theme;

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
                        Title = sender.Title,
                        Content = "/Editor/SaveChangesDialog/Content".GetLocalizedResource(),
                        PrimaryButtonText = "Save".GetLocalizedResource(),
                        SecondaryButtonText = "DoNotSave".GetLocalizedResource(),
                        CloseButtonText = "Cancel".GetLocalizedResource(),
                        DefaultButton = ContentDialogButton.Primary,
                        RequestedTheme = _themeService.Theme,
                        XamlRoot = XamlRoot
                    };
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        ViewModel.SaveTextCommand.Execute(null);
                    }
                    if (result == ContentDialogResult.Secondary)
                    {
                        _window?.Close();
                    }
                    _requestToClose = false;
                }
            }
        }

        private void EditorWindow_Closed(object sender, WindowEventArgs args)
        {
            _themeService.ThemeChanged -= ThemeService_ThemeChanged;

            if (_window is null)
            {
                return;
            }

            _window.AppWindow.Closing -= EditorWindow_Closing;
            _window.Closed -= EditorWindow_Closed;
            _window = null;
        }

        private void EditorTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            CharactersCountTextBlock.Text = "/Editor/CharactersCount/Text".GetLocalizedFormatResource(EditorTextBox.Text.Length);
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
                    ? "/Editor/SelectedCharactersCount/Text".GetLocalizedFormatResource(EditorTextBox.SelectionLength, EditorTextBox.Text.Length)
                    : "/Editor/CharactersCount/Text".GetLocalizedFormatResource(EditorTextBox.Text.Length);
        }

        private void PresenterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (_window?.AppWindow.Presenter is CompactOverlayPresenter)
            {
                _window.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                ((FontIcon)button.Content).Glyph = "\uE73F";
                ToolTipService.SetToolTip(button, "/Editor/CompactButton/ToolTipService/ToolTip".GetLocalizedResource());
            }
            else
            {
                _window?.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
                ((FontIcon)button.Content).Glyph = "\uE740";
                ToolTipService.SetToolTip(button, "/Editor/ExtendButton/ToolTipService/ToolTip".GetLocalizedResource());
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Bindings.StopTracking();
            DataContext = null;
        }
    }
}
