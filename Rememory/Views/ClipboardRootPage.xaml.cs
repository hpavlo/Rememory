using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Contracts;
using Rememory.Converters;
using Rememory.Helper;
using Rememory.Models;
using Rememory.ViewModels;
using Rememory.Views.Controls.Behavior;
using System;
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
        private Flyout PreviewTextFlyout => (Flyout)Resources["PreviewTextFlyout"];
        private Flyout PreviewRtfFlyout => (Flyout)Resources["PreviewRtfFlyout"];
        private Flyout PreviewImageFlyout => (Flyout)Resources["PreviewImageFlyout"];

        public ClipboardRootPage(ClipboardWindow window)
        {
            InitializeComponent();
            _window = window;
            _window.Showing += Window_Showing;
            _window.Hiding += Window_Hiding;
            _window.AppWindow.Closing += Window_Closing;

            RequestedTheme = ThemeService.Theme;
            ThemeService.ThemeChanged += ThemeChanged;

            ViewModel.SettingsContext.PropertyChanged += SettingsContext_PropertyChanged;
        }

        private void Window_Showing(object sender, EventArgs e)
        {
            if (ViewModel.SettingsContext.EnableSearchFocusOnStart)
            {
                SearchBox.Focus(FocusState.Keyboard);
            }
            else
            {
                ((UIElement)FocusManager.FindFirstFocusableElement(ClipsListView))?.Focus(FocusState.Programmatic);
            }

            if (ClipsListView.Items.Count != 0)
            {
                ClipsListView.ScrollIntoView(ClipsListView.Items.First());
            }

            ViewModel.OnWindowShowing();
        }

        private void Window_Hiding(object sender, EventArgs e)
        {
            ViewModel.OnWindowHiding();
        }

        private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            _window.Showing -= Window_Showing;
            _window.Hiding -= Window_Hiding;
            _window.AppWindow.Closing -= Window_Closing;
            ThemeService.ThemeChanged -= ThemeChanged;
            ViewModel.SettingsContext.PropertyChanged -= SettingsContext_PropertyChanged;
        }

        private void ThemeChanged(object? sender, ElementTheme theme)
        {
            RequestedTheme = theme;
        }

        private void ClipboardRootPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var oldStyle = (Style)Resources["DataPreviewFlyoutPresenterStyle"];
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

        private void SettingsContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs a)
        {
            // Swap tab indexes between SearchBox and ListView
            if ( string.Equals(a.PropertyName, nameof(ViewModel.SettingsContext.EnableSearchFocusOnStart)))
            {
                (ClipsListView.TabIndex, SearchBox.TabIndex) = (SearchBox.TabIndex, ClipsListView.TabIndex);
            }
        }

        #region Window moving

        private int startPointerX = 0, startPointerY = 0, startWindowX = 0, startWindowY = 0;
        private bool isWindowMoving = false;

        private void WindowDragArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsLeftButtonPressed && ViewModel.SettingsContext.ClipboardWindowPositionIndex != (int)ClipboardWindowPosition.Right)
            {
                ((UIElement)sender).CapturePointer(e.Pointer);
                startWindowX = _window.AppWindow.Position.X;
                startWindowY = _window.AppWindow.Position.Y;
                NativeHelper.GetCursorPos(out var pt);
                startPointerX = pt.X;
                startPointerY = pt.Y;
                isWindowMoving = true;
            }
        }

        private void WindowDragArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ((UIElement)sender).ReleasePointerCaptures();
            isWindowMoving = false;
            _window.AppWindow.Move(ClipboardWindow.AdjustWindowPositionToWorkArea(_window.AppWindow.Position, _window.AppWindow.Size));
            }

        private void WindowDragArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsLeftButtonPressed)
            {
                NativeHelper.GetCursorPos(out var pt);

                if (isWindowMoving)
                {
                    _window.AppWindow.Move(new(startWindowX + (pt.X - startPointerX), startWindowY + (pt.Y - startPointerY)));
                }
                e.Handled = true;
            }
        }

        #endregion

        #region Context menu items

        private void MenuFlyoutTags_Loaded(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutSubItem)sender;
            var clip = (ClipModel)menuItem.DataContext;
            var tags = ViewModel.GetTags();

            menuItem.IsEnabled = tags.Any();
            menuItem.Items.Clear();

            foreach (var tag in tags)
            {
                menuItem.Items.Add(new ToggleMenuFlyoutItem()
                {
                    Text = tag.Name,
                    IsChecked = clip.Tags.Contains(tag),
                    Command = ViewModel.ToggleClipTagCommand,
                    CommandParameter = new Tuple<ClipModel, TagModel>(clip, tag),
                    Icon = new FontIcon() { Glyph = "\uEA3B", Foreground = tag.ColorBrush }
                });
            }
        }

        #endregion

        #region Preview Flyout

        private void OpenInFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            OpenPreviewFlyout((ClipModel)((FrameworkElement)sender).DataContext);
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

        private void OpenPreviewFlyout(ClipModel clip)
        {
            foreach (var dataItem in clip.Data)
            {
                switch (dataItem.Key)
                {
                    case ClipboardFormat.Png:
                    case ClipboardFormat.Bitmap:
                        try
                        {
                            ((Image)PreviewImageFlyout.Content).Source = new BitmapImage(new(dataItem.Value.Data));
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
                            richEditBox.Document.SetText(TextSetOptions.FormatRtf, File.ReadAllText(dataItem.Value.Data).Replace("{\\rtf", "{\\rtf1"));
                            richEditBox.SearchHighligh(ViewModel.SearchString);
                            richEditBox.IsReadOnly = true;
                            PreviewRtfFlyout.ShowAt(this);
                            return;
                        }
                        catch { }
                        break;
                    case ClipboardFormat.Text:
                        var textBlock = (TextBlock)PreviewTextFlyout.Content;
                        textBlock.Text = dataItem.Value.Data;
                        textBlock.SearchHighlight(ViewModel.SearchString);
                        PreviewTextFlyout.ShowAt(this);
                        return;
                }
            }
        }

        #endregion

        #region KeyboardAccelerators

        // Open or close preview if user press 'space' button
        private void OpenInFlyoutKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            if (PreviewTextFlyout.IsOpen)
            {
                PreviewTextFlyout.Hide();
                return;
            }
            if (PreviewRtfFlyout.IsOpen)
            {
                PreviewRtfFlyout.Hide();
                return;
            }
            if (PreviewImageFlyout.IsOpen)
            {
                PreviewImageFlyout.Hide();
                return;
            }

            OpenPreviewFlyout((ClipModel)((FrameworkElement)args.Element).DataContext);
        }
        private void PastePlainTextKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var button = (Button)args.Element;
            if (ViewModel.PasteClipAsPlainTextCommand.CanExecute(button.DataContext))
            {
                KeyboardHelper.MultiKeyAction([VirtualKey.Shift], KeyboardHelper.KeyAction.Up);
                ViewModel.PasteClipAsPlainTextCommand.Execute(button.DataContext);
            }
            args.Handled = true;
        }

        private void CopyKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var button = (Button)args.Element;
            ViewModel.CopyClipCommand.Execute(button.DataContext);
            args.Handled = true;
        }

        private void EditKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var button = (Button)args.Element;
            if (ViewModel.EditClipCommand.CanExecute(button.DataContext))
            {
                ViewModel.EditClipCommand.Execute(button.DataContext);
            }
            args.Handled = true;
        }

        #endregion

        #region Apps filter

        private void FilterTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Works only for two layers tree view
            // It adds selected items from second (child) layer
            foreach (var app in ViewModel.RootAppNode.Children.Where(app => app.IsSelected))
            {
                FilterTreeView.SelectedItems.Add(app);
            }
        }

        private void FilterTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            // IsSelected property binding doesn't working for now
            // We should do it manually
            foreach (var item in args.AddedItems.Cast<AppTreeViewNode>())
            {
                item.IsSelected = true;
            }

            foreach (var item in args.RemovedItems.Cast<AppTreeViewNode>())
            {
                item.IsSelected = false;
            }

            ViewModel.OnFilterTreeViewSelectionChanged();
        }

        #endregion

        private void Escape_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                _window.HideWindow();
            }
        }

        private async void ClipsListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var clip = (ClipModel?)e.Items.FirstOrDefault();
            if (clip is not null) 
            {
                await ViewModel.OnDragClipStartingAsync(clip, e.Data);
            }
        }

        // Change preview if user select another item
        private void ClipButton_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PreviewTextFlyout.IsOpen || PreviewRtfFlyout.IsOpen || PreviewImageFlyout.IsOpen)
            {
                OpenPreviewFlyout((ClipModel)((FrameworkElement)sender).DataContext);
            }
        }
    }
}
