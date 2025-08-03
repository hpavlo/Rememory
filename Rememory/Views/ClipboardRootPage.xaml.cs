using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.ViewModels;
using Rememory.Views.Controls.Behavior;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

namespace Rememory.Views
{
    public sealed partial class ClipboardRootPage : Page
    {
        public readonly ClipboardRootPageViewModel ViewModel = new();

        /// <summary>
        /// Contains all selected clips ordered by selection time.
        /// </summary>
        public List<ClipModel> OrderedSelectedClips { get; private set; } = [];

        private readonly ClipboardWindow _window;
        private IThemeService ThemeService => App.Current.ThemeService;
        private Flyout PreviewTextFlyout => (Flyout)Resources["PreviewTextFlyout"];
        private Flyout PreviewRtfFlyout => (Flyout)Resources["PreviewRtfFlyout"];
        private Flyout PreviewImageFlyout => (Flyout)Resources["PreviewImageFlyout"];

        private readonly MenuFlyout _noneSelectionClipsContextMenu;
        private readonly MenuFlyout _multipleSelectionClipsContextMenu;

        public ClipboardRootPage(ClipboardWindow window)
        {
            InitializeComponent();
            _window = window;
            _window.Showing += Window_Showing;
            _window.Hiding += Window_Hiding;
            _window.AppWindow.Closing += Window_Closing;

            RequestedTheme = ThemeService.Theme;
            ChangeThemeBackgroundColor();
            ThemeService.ThemeChanged += ThemeService_ThemeChanged;
            ThemeService.WindowBackdropChanged += ThemeService_WindowBackdropChanged;

            ViewModel.SettingsContext.PropertyChanged += SettingsContext_PropertyChanged;
            ClipsListView.Items.VectorChanged += ClipsListView_Items_VectorChanged;

            _noneSelectionClipsContextMenu = (MenuFlyout)Resources["NoneSelectionClipsContextMenu"];
            _multipleSelectionClipsContextMenu = (MenuFlyout)Resources["MultipleSelectionClipsContextMenu"];

            SelectedClipsCountTextBlock.Text = "/Clipboard/SelectedClipsCount/Text".GetLocalizedFormatResource(ClipsListView.SelectedItems.Count);
        }

        private void Window_Showing(object sender, EventArgs e)
        {
            if (ViewModel.SettingsContext.IsSearchFocusOnStartEnabled)
            {
                SearchBox.Focus(FocusState.Keyboard);
            }
            else
            {
                SetFocusOnFirstClipInList();
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
            ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
            ThemeService.WindowBackdropChanged -= ThemeService_WindowBackdropChanged;
            ViewModel.SettingsContext.PropertyChanged -= SettingsContext_PropertyChanged;
            ClipsListView.Items.VectorChanged -= ClipsListView_Items_VectorChanged;
        }

        private void ThemeService_ThemeChanged(object? sender, ElementTheme theme)
        {
            RequestedTheme = theme;
            ChangeThemeBackgroundColor();
        }

        private void ThemeService_WindowBackdropChanged(object? sender, WindowBackdropType e) => ChangeThemeBackgroundColor();

        private void ChangeThemeBackgroundColor()
        {
            SolidColorBrush newBackgroundBrush = new();
            if (ThemeService.WindowBackdrop == WindowBackdropType.None)
            {
                newBackgroundBrush.Color = ThemeService.Theme switch
                {
                    ElementTheme.Light => Colors.White,
                    ElementTheme.Dark => Colors.Black,
                    _ => NativeHelper.ShouldSystemUseDarkMode() ? Colors.Black : Colors.White,
                };
            }
            Background = newBackgroundBrush;
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

        private void SettingsContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs a)
        {
            // Swap tab indexes between SearchBox and ListView
            if (string.Equals(a.PropertyName, nameof(ViewModel.SettingsContext.IsSearchFocusOnStartEnabled)))
            {
                (ClipsListView.TabIndex, SearchBox.TabIndex) = (SearchBox.TabIndex, ClipsListView.TabIndex);
            }
        }

        private void RootPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                _window.HideWindow();
            }
        }

        private void SetFocusOnFirstClipInList() => ((UIElement)FocusManager.FindFirstFocusableElement(ClipsListView))?.Focus(FocusState.Programmatic);

        #region Window moving

        private PointerUpdateKind _lastPointerClickKind;
        private int _startPointerX = 0, _startPointerY = 0, _startWindowX = 0, _startWindowY = 0;
        private bool _isWindowMoving = false;

        private void WindowDragArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;

            if ((_lastPointerClickKind = properties.PointerUpdateKind) != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }
            
            if (ViewModel.SettingsContext.WindowPosition != ClipboardWindowPosition.Right)
            {
                ((UIElement)sender).CapturePointer(e.Pointer);
                _startWindowX = _window.AppWindow.Position.X;
                _startWindowY = _window.AppWindow.Position.Y;
                NativeHelper.GetCursorPos(out var pt);
                _startPointerX = pt.X;
                _startPointerY = pt.Y;
                _isWindowMoving = true;
            }
        }

        private void WindowDragArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ((UIElement)sender).ReleasePointerCaptures();
            _isWindowMoving = false;
            _window.AppWindow.Move(ClipboardWindow.AdjustWindowPositionToWorkArea(_window.AppWindow.Position, _window.AppWindow.Size));
        }

        private void WindowDragArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsLeftButtonPressed)
            {
                NativeHelper.GetCursorPos(out var pt);

                if (_isWindowMoving && (Math.Abs(_startPointerX - pt.X) > 5 || Math.Abs(_startPointerY - pt.Y) > 5))
                {
                    _window.AppWindow.Move(new(_startWindowX + (pt.X - _startPointerX), _startWindowY + (pt.Y - _startPointerY)));
                }
                e.Handled = true;
            }
        }

        private void WindowDragArea_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_lastPointerClickKind == PointerUpdateKind.LeftButtonPressed
                && ViewModel.ToggleWindowPinnedCommand.CanExecute(null))
            {
                ViewModel.ToggleWindowPinnedCommand.Execute(null);
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

        #region Clips list

        private void ClipsListView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.OriginalSource is not ListViewItem clipItem)
            {
                return;
            }

            bool isCtrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool isShiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            switch (e.Key)
            {
                // Up
                case VirtualKey.Up when clipItem.Content == ClipsListView.Items.First():
                    SearchBox.Focus(FocusState.Programmatic);
                    return;
                // Left
                case VirtualKey.Left:
                    NavigationTabList.Focus(FocusState.Programmatic);
                    return;
                // Ctrl + C
                case VirtualKey.C when isCtrlPressed:
                    if (ClipsListView.SelectionMode == ListViewSelectionMode.None)
                    {
                        if (ViewModel.CopyClipCommand.CanExecute(clipItem.Content))
                        {
                            ViewModel.CopyClipCommand.Execute(clipItem.Content);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        if (ViewModel.CopyClipsCommand.CanExecute(OrderedSelectedClips))
                        {
                            ViewModel.CopyClipsCommand.Execute(OrderedSelectedClips);
                            e.Handled = true;
                        }
                    }
                    return;
            }

            if (ClipsListView.SelectionMode != ListViewSelectionMode.None)
            {
                return;
            }

            switch (e.Key)
            {
                // Shift + Enter
                case VirtualKey.Enter when isShiftPressed:
                    if (ViewModel.PasteClipAsPlainTextCommand.CanExecute(clipItem.Content))
                    {
                        KeyboardHelper.MultiKeyAction([VirtualKey.Shift], KeyboardHelper.KeyAction.Up);
                        ViewModel.PasteClipAsPlainTextCommand.Execute(clipItem.Content);
                        e.Handled = true;
                    }
                    break;
                // Enter
                case VirtualKey.Enter:
                    if (ViewModel.PasteClipCommand.CanExecute(clipItem.Content))
                    {
                        ViewModel.PasteClipCommand.Execute(clipItem.Content);
                        e.Handled = true;
                    }
                    break;
                // Ctrl + U
                case VirtualKey.U when isCtrlPressed:
                    if (ViewModel.EditClipCommand.CanExecute(clipItem.Content))
                    {
                        ViewModel.EditClipCommand.Execute(clipItem.Content);
                        e.Handled = true;
                    }
                    break;
                // Space
                case VirtualKey.Space:
                    e.Handled = true;
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
                    OpenPreviewFlyout((ClipModel)clipItem.Content);
                    break;
            }
        }

        private void ClipsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Item is not ClipModel clipModel)
            {
                return;
            }

            args.ItemContainer.SetBinding(IsEnabledProperty, new Binding()
            {
                Source = clipModel,
                Mode = BindingMode.OneWay,
                Path = new(nameof(clipModel.IsOpenInEditor)),
                Converter = (IValueConverter)Resources["BoolNegationConverter"]
            });
        }

        private void ClipsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ClipsListView.SelectionMode == ListViewSelectionMode.None
                && ViewModel.PasteClipCommand.CanExecute(e.ClickedItem))
            {
                ViewModel.PasteClipCommand.Execute(e.ClickedItem);
            }
        }

        private void ClipsListView_GettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            if (PreviewTextFlyout.IsOpen || PreviewRtfFlyout.IsOpen || PreviewImageFlyout.IsOpen)
            {
                OpenPreviewFlyout((ClipModel)((ListViewItem)args.NewFocusedElement).Content);
            }
        }

        private async void ClipsListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var draggedClip = (ClipModel?)e.Items.FirstOrDefault();
            if (draggedClip is null)
            {
                return;
            }

            if (ClipsListView.SelectionMode == ListViewSelectionMode.None
                || ClipsListView.SelectionMode == ListViewSelectionMode.Multiple && !OrderedSelectedClips.Contains(draggedClip))
            {
                await ViewModel.OnDragClipStartingAsync(draggedClip, e.Data);
            }
            else if (ClipsListView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                await ViewModel.OnDragMultipleClipsStartingAsync(OrderedSelectedClips, e.Data);
            }
        }

        private void ClipsListView_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
        {
            // To check if new Clip was inserted to ClipsListView
            if (args.CollectionChange == CollectionChange.ItemInserted)
            {
                TriggerMultipleSelectionFooterUpdate();
            }
        }

        private void ClipsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (ClipModel removedClip in e.RemovedItems.Cast<ClipModel>())
            {
                OrderedSelectedClips.Remove(removedClip);
            }
            OrderedSelectedClips.AddRange(e.AddedItems.Cast<ClipModel>());
            TriggerMultipleSelectionFooterUpdate();
        }

        private void TriggerMultipleSelectionFooterUpdate()
        {
            SelectAllCheckBox.IsChecked = ClipsListView.SelectedItems.Count switch
            {
                0 => false,
                var count when count == ViewModel.ClipsCollection.Count => true,
                _ => null
            };

            SelectedClipsCountTextBlock.Text = "/Clipboard/SelectedClipsCount/Text".GetLocalizedFormatResource(ClipsListView.SelectedItems.Count);
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked ?? true)
            {
                ClipsListView.SelectAll();
            }
            else
            {
                ClipsListView.DeselectAll();
            }
        }

        private void ClipRootGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var clipModel = (ClipModel)((FrameworkElement)sender).DataContext;
            bool useMultipleContextMenu = ClipsListView.SelectionMode == ListViewSelectionMode.Multiple && OrderedSelectedClips.Contains(clipModel);
            var menuFlyout = useMultipleContextMenu ? _multipleSelectionClipsContextMenu : _noneSelectionClipsContextMenu;

            if (args.TryGetPosition(sender, out var point))
            {
                menuFlyout.ShowAt(sender, point);
            }
            else
            {
                menuFlyout.ShowAt((FrameworkElement)sender);
            }
        }

        #endregion

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Down)
            {
                SetFocusOnFirstClipInList();
            }
        }

        private void NavigationTabList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Right)
            {
                SetFocusOnFirstClipInList();
            }
        }
    }
}
