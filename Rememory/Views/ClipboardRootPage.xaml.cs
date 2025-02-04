using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Converters;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Service;
using Rememory.ViewModels;
using Rememory.Views.Controls.Behavior;
using Rememory.Views.Settings;
using System.IO;
using System.Linq;
using Windows.System;

namespace Rememory.Views
{
    public sealed partial class ClipboardRootPage : Page
    {
        public readonly ClipboardRootPageViewModel ViewModel = new();

        private readonly ClipboardWindow _window;
        private IThemeService ThemeService => App.Current.ThemeService;
        private Flyout PreviewTextFlyout => (Flyout)this.Resources["PreviewTextFlyout"];
        private Flyout PreviewRtfFlyout => (Flyout)this.Resources["PreviewRtfFlyout"];
        private Flyout PreviewImageFlyout => (Flyout)this.Resources["PreviewImageFlyout"];

        public ClipboardRootPage(ClipboardWindow window)
        {
            this.InitializeComponent();
            _window = window;
            _window.Showing += Window_Showing;
            _window.Hiding += Window_Hiding;

            RequestedTheme = ThemeService.Theme;
            ThemeService.ThemeChanged += ThemeChanged;

            SizeChanged += ClipboardRootPage_SizeChanged;
        }

        private void Window_Showing(object sender, System.EventArgs e)
        {
            ((UIElement)FocusManager.FindFirstFocusableElement(this))?.Focus(FocusState.Programmatic);
            if (ClipboardItemListView.Items.Count != 0)
            {
                ClipboardItemListView.ScrollIntoView(ClipboardItemListView.Items.First());
            }

            ViewModel.OnWindowShowing();
        }

        private void Window_Hiding(object sender, System.EventArgs e)
        {
            ViewModel.OnWindowHiding();
        }

        private void ThemeChanged(object sender, ElementTheme theme)
        {
            this.RequestedTheme = theme;
        }

        private void ClipboardRootPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var oldStyle = (Style)this.Resources["DataPreviewFlyoutPresenterStyle"];
            Style UpdateFlyoutPresenterStyle(double maxWidth, double maxHeight)
            {
                Style newStyle = new(typeof(FlyoutPresenter));
                foreach (var setter in oldStyle.Setters.Cast<Setter>())
                {
                    if (setter.Property == MaxWidthProperty)
                    {
                        newStyle.Setters.Add(new Setter(MaxWidthProperty, maxWidth));
                        continue;
                    }
                    if (setter.Property == MaxHeightProperty)
                    {
                        newStyle.Setters.Add(new Setter(MaxHeightProperty, maxHeight));
                        continue;
                    }
                    newStyle.Setters.Add(setter);
                }
                return newStyle;
            }

            PreviewRtfFlyout.FlyoutPresenterStyle = UpdateFlyoutPresenterStyle(ActualWidth * 1.5, ActualHeight);
            ((RichEditBox)PreviewRtfFlyout.Content).Width = ActualWidth * 1.5 - 36;

            PreviewTextFlyout.FlyoutPresenterStyle = UpdateFlyoutPresenterStyle(ActualWidth, ActualHeight);
            ((TextBlock)PreviewTextFlyout.Content).MaxWidth = ActualWidth - 36;

            PreviewImageFlyout.FlyoutPresenterStyle = UpdateFlyoutPresenterStyle(ActualWidth * 2, ActualHeight);
            ((Image)PreviewImageFlyout.Content).SetValue(ImageAutoResizeBehavior.MaxImageWidthProperty, ActualWidth * 2 - 36);
            ((Image)PreviewImageFlyout.Content).SetValue(ImageAutoResizeBehavior.MaxImageHeightProperty, ActualHeight - 34);
        }

        private void NavigationSelectorBar_Loaded(object sender, RoutedEventArgs e)
        {
            var binding = new Binding()
            {
                Source = DataContext,
                Path = new("SelectedMenuItem"),
                Converter = new NavigationSelectedMenuItemConverter(),
                ConverterParameter = sender,
                Mode = BindingMode.TwoWay
            };
            ((FrameworkElement)sender).SetBinding(SelectorBar.SelectedItemProperty, binding);
        }

        #region Context menu items

        private void FavoriteMenuItem_Loading(FrameworkElement sender, object args)
        {
            UpdateFavoriteMenuFlyoutItem((MenuFlyoutItem)sender);
        }

        private void FavoriteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutItem)sender;
            ViewModel.ChangeItemFavoriteCommand.Execute(menuItem.DataContext);
            UpdateFavoriteMenuFlyoutItem(menuItem);
        }

        private void UpdateFavoriteMenuFlyoutItem(MenuFlyoutItem menuItem)
        {
            if (((ClipboardItem)menuItem.DataContext).IsFavorite)
            {
                menuItem.Text = "ContextMenu_RemoveFromFavorite/Text".GetLocalizedResource();
                menuItem.Icon = new FontIcon() { Glyph = "\uE8D9" };
            }
            else
            {
                menuItem.Text = "ContextMenu_AddToFavorite/Text".GetLocalizedResource();
                menuItem.Icon = new FontIcon() { Glyph = "\uE734" };
            }
        }

        #endregion

        #region Preview Flyout

        private void OpenInFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            OpenPreviewFlyout((ClipboardItem)((FrameworkElement)sender).DataContext);
        }

        private void PreviewFlyout_Closed(object sender, object e)
        {
            switch (((Flyout)sender).Content)
            {
                case Image flyoutImage:
                    flyoutImage.Source = null;
                    break;
                case RichEditBox flyoutRtf:
                    flyoutRtf.IsReadOnly = false;
                    flyoutRtf.Document.SetText(TextSetOptions.FormatRtf, string.Empty);
                    break;
                case TextBlock flyoutText:
                    flyoutText.Text = string.Empty;
                    flyoutText.TextHighlighters.Clear();
                    break;
            }
        }

        private void OpenPreviewFlyout(ClipboardItem clipboardItem)
        {
            foreach (var data in clipboardItem.DataMap)
            {
                switch (data.Key)
                {
                    case ClipboardFormat.Png:
                        try
                        {
                            ((Image)PreviewImageFlyout.Content).Source = new BitmapImage(new(data.Value));
                            PreviewImageFlyout.ShowAt(this);
                            return;
                        }
                        catch { }
                        break;
                    case ClipboardFormat.Rtf:
                        var richEditBox = (RichEditBox)PreviewRtfFlyout.Content;
                        richEditBox.IsReadOnly = false;
                        try
                        {
                            richEditBox.Document.SetText(TextSetOptions.FormatRtf, File.ReadAllText(data.Value).Replace("{\\rtf", "{\\rtf1"));
                            richEditBox.SearchHighligh(ViewModel.SearchString);
                            richEditBox.IsReadOnly = true;
                            PreviewRtfFlyout.ShowAt(this);
                            return;
                        }
                        catch { }
                        break;
                    case ClipboardFormat.Text:
                        var textBlock = (TextBlock)PreviewTextFlyout.Content;
                        textBlock.Text = data.Value;
                        textBlock.SearchHighlight(ViewModel.SearchString);
                        PreviewTextFlyout.ShowAt(this);
                        return;
                }
            }
        }

        #endregion

        #region KeyboardAccelerators

        private void OpenInFlyoutKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            OpenPreviewFlyout((ClipboardItem)((FrameworkElement)args.Element).DataContext);
            args.Handled = true;
        }
        private void PastePlainTextKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var button = (Button)args.Element;
            if (ViewModel.PastePlainTextItemCommand.CanExecute(button.DataContext))
            {
                KeyboardHelper.MultiKeyAction([VirtualKey.Shift], KeyboardHelper.KeyAction.Up);
                ViewModel.PastePlainTextItemCommand.Execute(button.DataContext);
            }
            args.Handled = true;
        }

        private void CopyKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var button = (Button)args.Element;
            ViewModel.CopyItemCommand.Execute(button.DataContext);
            args.Handled = true;
        }

        private void EditKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var button = (Button)args.Element;
            if (ViewModel.EditItemCommand.CanExecute(button.DataContext))
            {
                ViewModel.EditItemCommand.Execute(button.DataContext);
            }
            args.Handled = true;
        }

        #endregion

        private void Escape_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                App.Current.ClipboardWindow.HideWindow();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow.ShowSettingsWindow();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.ClipboardWindow.HideWindow();
        }

        private void ClipboardItemListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var item = (ClipboardItem)e.Items.FirstOrDefault();
            if (item is not null)
            {
                ViewModel.OnDragItemStarting(item, e.Data);
            }
        }
    }
}
