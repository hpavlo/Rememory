using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Converters;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Core;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.ViewModels;
using Rememory.Views.Clipboard.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

namespace Rememory.Views.Clipboard
{
    public sealed partial class ClipboardRootPage : Page
    {
        public readonly ClipboardRootPageViewModel ViewModel = new();
        public Border WindowCaptionArea => WindowCaptionBorder;

        /// <summary>
        /// Contains all selected clips ordered by selection time.
        /// </summary>
        public List<ClipModel> OrderedSelectedClips { get; private set; } = [];

        private readonly IThemeService _themeService = App.Current.ThemeService;
        private readonly ClipboardWindow _window = App.Current.ClipboardWindow;
        private readonly ClipPreviewFlyout _clipPreviewFlyout = new();
        private readonly BoolNegationConverter _boolNegationConverter = new();

        private readonly DataTemplate _clipLayoutTemplate;
        private readonly DataTemplate _compactClipLayoutTemplate;

        private readonly MenuFlyout _singleClipContextMenu;
        private readonly MenuFlyout _multipleClipsContextMenu;

        // Prevents repeated clip deletions when holding Shift+Delete
        private bool _deleteShortcutHandled = false;

        private readonly Dictionary<ClipboardFormat, string> _saveClipMenuItems = new()
        {
            { ClipboardFormat.Text, "Text" },
            { ClipboardFormat.Rtf, "RTF" },
            { ClipboardFormat.Html, "HTML" },
            { ClipboardFormat.Png, "PNG" },
            { ClipboardFormat.Bitmap, "Bitmap" }
        };

        public ClipboardRootPage()
        {
            InitializeComponent();
            _window.Showing += Window_Showing;
            _window.Hiding += Window_Hiding;
            _window.AppWindow.Closing += Window_Closing;

            RequestedTheme = _themeService.Theme;
            TriggerThemeBackgroundColor();
            _themeService.ThemeChanged += ThemeService_ThemeChanged;
            _themeService.WindowBackdropChanged += ThemeService_WindowBackdropChanged;

            ViewModel.SettingsContext.PropertyChanged += SettingsContext_PropertyChanged;
            ClipsListView.Items.VectorChanged += ClipsListView_Items_VectorChanged;

            _clipLayoutTemplate = (DataTemplate)Resources["ClipLayoutTemplate"];
            _compactClipLayoutTemplate = (DataTemplate)Resources["CompactClipLayoutTemplate"];

            _singleClipContextMenu = (MenuFlyout)Resources["SingleClipContextMenu"];
            _multipleClipsContextMenu = (MenuFlyout)Resources["MultipleClipsContextMenu"];

            SelectedClipsCountTextBlock.Text = "/Clipboard/SelectedClipsCount/Text".GetLocalizedFormatResource(ClipsListView.SelectedItems.Count);
            TriggerClipLayoutTemplate();
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
            _themeService.ThemeChanged -= ThemeService_ThemeChanged;
            _themeService.WindowBackdropChanged -= ThemeService_WindowBackdropChanged;
            ViewModel.SettingsContext.PropertyChanged -= SettingsContext_PropertyChanged;
            ClipsListView.Items.VectorChanged -= ClipsListView_Items_VectorChanged;
        }

        private void ThemeService_ThemeChanged(IThemeService sender, ElementTheme theme)
        {
            RequestedTheme = theme;
            TriggerThemeBackgroundColor();
        }

        private void ThemeService_WindowBackdropChanged(IThemeService sender, WindowBackdropType e) => TriggerThemeBackgroundColor();

        private void TriggerThemeBackgroundColor()
        {
            SolidColorBrush newBackgroundBrush = new();
            if (_themeService.WindowBackdrop == WindowBackdropType.None)
            {
                newBackgroundBrush.Color = _themeService.Theme switch
                {
                    ElementTheme.Light => Colors.White,
                    ElementTheme.Dark => Colors.Black,
                    _ => NativeHelper.ShouldSystemUseDarkMode() ? Colors.Black : Colors.White,
                };
            }
            Background = newBackgroundBrush;
        }

        private void SettingsContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs a)
        {
            switch (a.PropertyName)
            {
                case nameof(ViewModel.SettingsContext.IsSearchFocusOnStartEnabled):
                    // Swap tab indexes between SearchBox and ListView
                    (ClipsListView.TabIndex, SearchBox.TabIndex) = (SearchBox.TabIndex, ClipsListView.TabIndex);
                    break;
                case nameof(ViewModel.SettingsContext.IsCompactViewEnabled):
                    TriggerClipLayoutTemplate();
                    break;
            }
        }

        private void TriggerClipLayoutTemplate()
        {
            ClipsListView.ItemTemplate = ViewModel.SettingsContext.IsCompactViewEnabled ? _compactClipLayoutTemplate : _clipLayoutTemplate;
        }

        // Do not use preview key down event to avoid window hiding if key pressed on flyout
        private void RootPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                _window.HideWindow();
            }
        }

        private void SetFocusOnFirstClipInList()
        {
            var firstClipContainer = ClipsListView.ContainerFromIndex(0) as UIElement;

            firstClipContainer?.StartBringIntoView(new() { AnimationDesired = false });
            firstClipContainer?.Focus(FocusState.Programmatic);
        }

        #region Context menu items

        private void MenuFlyoutSaveClip_Loaded(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutSubItem)sender;
            var clip = (ClipModel)menuItem.DataContext;

            menuItem.Items.Clear();

            foreach (var menuItemPair in _saveClipMenuItems)
            {
                if (clip.Data.ContainsKey(menuItemPair.Key))
                {
                    menuItem.Items.Add(new MenuFlyoutItem()
                    {
                        Text = menuItemPair.Value,
                        Command = ViewModel.SaveClipDataCommand,
                        CommandParameter = new Tuple<ClipModel, ClipboardFormat>(clip, menuItemPair.Key)
                    });
                }
            }

            menuItem.IsEnabled = menuItem.Items.Count > 0;
        }

        private void MenuFlyoutTags_Loaded(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutSubItem)sender;
            var clip = (ClipModel)menuItem.DataContext;
            var tags = ViewModel.GetTags();

            menuItem.IsEnabled = tags.Any();
            menuItem.Items.Clear();

            foreach (var tag in tags)
            {
                var tagItem = new ToggleMenuFlyoutItem()
                {
                    Text = tag.Name,
                    Icon = new FontIcon() { Glyph = "\uEA3B", Foreground = tag.ColorBrush }
                };

                if (ClipsListView.SelectionMode == ListViewSelectionMode.None
                    || ClipsListView.SelectionMode == ListViewSelectionMode.Multiple && !OrderedSelectedClips.Contains(clip))
                {
                    tagItem.IsChecked = clip.Tags.Contains(tag);
                    tagItem.Command = ViewModel.ToggleClipTagCommand;
                    tagItem.CommandParameter = new Tuple<ClipModel, TagModel, bool>(clip, tag, tagItem.IsChecked);
                }
                else if (ClipsListView.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    tagItem.IsChecked = OrderedSelectedClips.All(clip => clip.Tags.Contains(tag));
                    tagItem.Command = ViewModel.ToggleClipsTagCommand;
                    tagItem.CommandParameter = new Tuple<IEnumerable<ClipModel>, TagModel, bool>(OrderedSelectedClips, tag, tagItem.IsChecked);
                }

                menuItem.Items.Add(tagItem);
            }

            // Toggle bottom separator item visibility
            if (ClipsListView.SelectionMode == ListViewSelectionMode.None)
            {
                ClipMenuFlyoutBottomSeparator.Visibility = menuItem.IsEnabled || FilterClipMenuFlyoutItem.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                ClipsMenuFlyoutBottomSeparator.Visibility = menuItem.IsEnabled || FilterClipsMenuFlyoutItem.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OpenInFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var clip = ((FrameworkElement)sender).DataContext as ClipModel;
            _clipPreviewFlyout.ShowDataPreview(clip, ViewModel.SearchString, this);
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

        private void FilterTreeView_Unloaded(object sender, RoutedEventArgs e)
        {
            FilterTreeView.SelectedItems.Clear();
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
                case VirtualKey.Up when clipItem.Content == ClipsListView.Items.FirstOrDefault():
                    SearchBox.Focus(FocusState.Programmatic);
                    return;
                // Left
                case VirtualKey.Left:
                    if (_clipPreviewFlyout.IsOpen)
                    {
                        _clipPreviewFlyout.ShowPreviousFormatPreview();
                        return;
                    }

                    var selectedItemContainer = NavigationTabView.ContainerFromMenuItem(NavigationTabView.SelectedItem) as UIElement;
                    selectedItemContainer?.Focus(FocusState.Programmatic);
                    return;
                case VirtualKey.Right:
                    if (_clipPreviewFlyout.IsOpen)
                    {
                        _clipPreviewFlyout.ShowNextFormatPreview();
                    }
                    return;
            }

            if (OrderedSelectedClips.Count > 0)
            {
                return;
            }

            switch (e.Key)
            {
                // Ctrl + C
                case VirtualKey.C when isCtrlPressed:
                    if (ViewModel.CopyClipCommand.CanExecute(clipItem.Content))
                    {
                        ViewModel.CopyClipCommand.Execute(clipItem.Content);
                        e.Handled = true;
                    }
                    return;
                // Ctrl + U
                case VirtualKey.U when isCtrlPressed:
                    if (ViewModel.EditClipCommand.CanExecute(clipItem.Content))
                    {
                        ViewModel.EditClipCommand.Execute(clipItem.Content);
                        e.Handled = true;
                    }
                    break;
                // Shift + Delete
                case VirtualKey.Delete when isShiftPressed:
                    if (!_deleteShortcutHandled && ViewModel.DeleteClipCommand.CanExecute(clipItem.Content))
                    {
                        ViewModel.DeleteClipCommand.Execute(clipItem.Content);
                        e.Handled = true;
                        _deleteShortcutHandled = true;
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
                // Space
                case VirtualKey.Space:
                    e.Handled = true;
                    if (_clipPreviewFlyout.IsOpen)
                    {
                        _clipPreviewFlyout.Hide();
                        return;
                    }

                    var clip = clipItem.Content as ClipModel;
                    _clipPreviewFlyout.ShowDataPreview(clip, ViewModel.SearchString, this);
                    break;
            }
        }

        private void ClipsListView_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Delete)
            {
                _deleteShortcutHandled = false;
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
                Converter = _boolNegationConverter
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
            if (!_clipPreviewFlyout.IsOpen)
            {
                return;
            }

            if (args.NewFocusedElement is ListViewItem { Content: ClipModel clip })
            {
                _clipPreviewFlyout.ShowDataPreview(clip, ViewModel.SearchString, this);
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
            EmptyListInfoPanel.Visibility = sender.Count > 0 || ViewModel.InSearchMode
                ? Visibility.Collapsed
                : Visibility.Visible;

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
            var menuFlyout = useMultipleContextMenu ? _multipleClipsContextMenu : _singleClipContextMenu;

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

        private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Down:
                    SetFocusOnFirstClipInList();
                    e.Handled = true;
                    break;
                case VirtualKey.Escape:
                    _window.HideWindow();
                    e.Handled = true;
                    break;
            }
        }

        private void NavigationTabList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Right && e.OriginalSource is NavigationViewItem)
            {
                SetFocusOnFirstClipInList();
            }
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SettingsContext.SkipWarningMessageOnMainWindowClipsErase)
            {
                if (ViewModel.EraseClipsOnSelectedTabCommand.CanExecute(null))
                {
                    ViewModel.EraseClipsOnSelectedTabCommand.Execute(null);
                }
            }
            else
            {
                EraseButtonFlyout.ShowAt(sender as FrameworkElement);
            }
        }

        private void EraseFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            EraseButtonFlyout.Hide();

            if (ViewModel.EraseClipsOnSelectedTabCommand.CanExecute(null))
            {
                ViewModel.EraseClipsOnSelectedTabCommand.Execute(null);
            }
        }
    }
}
