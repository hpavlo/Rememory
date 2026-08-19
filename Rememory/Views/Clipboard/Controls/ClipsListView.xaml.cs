using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Rememory.Core;
using Rememory.Helper;
using Rememory.Models;
using Rememory.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class ClipsListView : UserControl
    {
        private readonly ClipPreviewFlyout _clipPreviewFlyout = new();
        private readonly BoolNegationConverter _boolNegationConverter = new();
        private readonly Dictionary<ClipboardFormat, string> _saveClipMenuItems = new()
        {
            { ClipboardFormat.Text, "Text" },
            { ClipboardFormat.Rtf, "RTF" },
            { ClipboardFormat.Html, "HTML" },
            { ClipboardFormat.Png, "PNG" },
            { ClipboardFormat.Bitmap, "Bitmap" }
        };

        private readonly MenuFlyout _singleClipContextMenu;
        private readonly MenuFlyout _multipleClipsContextMenu;
        private readonly DataTemplate _compactClipLayoutTemplate;
        private readonly DataTemplate _clipLayoutTemplate;

        // Prevents repeated clip deletions when holding Shift+Delete
        private bool _deleteShortcutHandled = false;

        public event EventHandler? RequestSearchBoxFocus;
        public event EventHandler? RequestNavigationTabViewFocus;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(ClipboardRootPageViewModel), typeof(ClipsListView), new PropertyMetadata(null));
        public ClipboardRootPageViewModel ViewModel
        {
            get => (ClipboardRootPageViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(nameof(SelectionMode), typeof(ListViewSelectionMode), typeof(ClipsListView), new PropertyMetadata(ListViewSelectionMode.None));

        public ListViewSelectionMode SelectionMode
        {
            get => (ListViewSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public static readonly DependencyProperty ShowInCompactModeProperty =
            DependencyProperty.Register(nameof(ShowInCompactMode), typeof(bool), typeof(ClipsListView), new PropertyMetadata(false, ShowInCompactModePropertyChanged));

        public bool ShowInCompactMode
        {
            get => (bool)GetValue(ShowInCompactModeProperty);
            set => SetValue(ShowInCompactModeProperty, value);
        }

        /// <summary>
        /// Contains selected clips ordered by selection time.
        /// Preserves the user's selection order for multi-item drag-and-drop operations.
        /// </summary>
        public List<ClipModel> OrderedSelectedClips { get; private set; } = [];

        public ClipsListView()
        {
            InitializeComponent();

            _singleClipContextMenu = (MenuFlyout)Resources["SingleClipContextMenu"];
            _multipleClipsContextMenu = (MenuFlyout)Resources["MultipleClipsContextMenu"];
            _compactClipLayoutTemplate = (DataTemplate)Resources["CompactClipLayoutTemplate"];
            _clipLayoutTemplate = (DataTemplate)Resources["ClipLayoutTemplate"];

            ClipsList.Items.VectorChanged += ClipsList_Items_VectorChanged;
            TriggerMultipleSelectionFooterUpdate();
        }

        public void SetClipFocusedByIndex(int index)
        {
            var firstClipContainer = ClipsList.ContainerFromIndex(index) as UIElement;

            firstClipContainer?.StartBringIntoView(new() { AnimationDesired = false });
            firstClipContainer?.Focus(FocusState.Programmatic);
        }

        public void ScrollUpTheList()
        {
            if (ClipsList.Items.Count > 0)
            {
                ClipsList.ScrollIntoView(ClipsList.Items.First());
            }
        }

        private static void ShowInCompactModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ClipsListView clipsListView)
            {
                clipsListView.ClipsList.ItemTemplate = e.NewValue is true
                    ? clipsListView._compactClipLayoutTemplate
                    : clipsListView._clipLayoutTemplate;
            }
        }

        private void RootControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ClipsList.Items.VectorChanged -= ClipsList_Items_VectorChanged;
        }

        private void ClipRootGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var clipModel = (ClipModel)((FrameworkElement)sender).DataContext;
            bool useMultipleContextMenu = ClipsList.SelectionMode == ListViewSelectionMode.Multiple && OrderedSelectedClips.Contains(clipModel);
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

        private void ClipsList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
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
                case VirtualKey.Up when clipItem.Content == ClipsList.Items.FirstOrDefault():
                    RequestSearchBoxFocus?.Invoke(this, EventArgs.Empty);
                    return;
                // Left
                case VirtualKey.Left:
                    if (_clipPreviewFlyout.IsOpen)
                    {
                        _clipPreviewFlyout.ShowPreviousFormatPreview();
                        return;
                    }

                    RequestNavigationTabViewFocus?.Invoke(this, EventArgs.Empty);
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

            if (ClipsList.SelectionMode != ListViewSelectionMode.None)
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

        private void ClipsList_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Delete)
            {
                _deleteShortcutHandled = false;
            }
        }

        private void ClipsList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
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

        private void ClipsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ClipsList.SelectionMode == ListViewSelectionMode.None
                && ViewModel.PasteClipCommand.CanExecute(e.ClickedItem))
            {
                ViewModel.PasteClipCommand.Execute(e.ClickedItem);
            }
        }

        private void ClipsList_GettingFocus(UIElement sender, GettingFocusEventArgs args)
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

        private async void ClipsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.FirstOrDefault() is not ClipModel draggedClip)
            {
                return;
            }

            if (ClipsList.SelectionMode == ListViewSelectionMode.None
                || ClipsList.SelectionMode == ListViewSelectionMode.Multiple && !OrderedSelectedClips.Contains(draggedClip))
            {
                await ViewModel.OnDragClipStartingAsync(draggedClip, e.Data);
            }
            else if (ClipsList.SelectionMode == ListViewSelectionMode.Multiple)
            {
                await ViewModel.OnDragMultipleClipsStartingAsync(OrderedSelectedClips, e.Data);
            }
        }

        private void ClipsList_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
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

        private void ClipsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            var selectedClipsCount = OrderedSelectedClips.Count;
            SelectAllCheckBox.IsChecked = selectedClipsCount switch
            {
                0 => false,
                var count when count == ViewModel.ClipsCollection.Count => true,
                _ => null
            };

            SelectedClipsCountTextBlock.Text = "/Clipboard/SelectedClipsCount/Text".GetLocalizedFormatResource(selectedClipsCount);
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked is true)
            {
                ClipsList.SelectAll();
            }
            else if (SelectAllCheckBox.IsChecked is false)
            {
                ClipsList.DeselectAll();
            }
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

                if (ClipsList.SelectionMode == ListViewSelectionMode.None
                    || ClipsList.SelectionMode == ListViewSelectionMode.Multiple && !OrderedSelectedClips.Contains(clip))
                {
                    tagItem.IsChecked = clip.Tags.Contains(tag);
                    tagItem.Command = ViewModel.ToggleClipTagCommand;
                    tagItem.CommandParameter = new Tuple<ClipModel, TagModel, bool>(clip, tag, tagItem.IsChecked);
                }
                else if (ClipsList.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    tagItem.IsChecked = OrderedSelectedClips.All(clip => clip.Tags.Contains(tag));
                    tagItem.Command = ViewModel.ToggleClipsTagCommand;
                    tagItem.CommandParameter = new Tuple<IEnumerable<ClipModel>, TagModel, bool>(OrderedSelectedClips, tag, tagItem.IsChecked);
                }

                menuItem.Items.Add(tagItem);
            }

            // Toggle bottom separator item visibility
            if (ClipsList.SelectionMode == ListViewSelectionMode.None)
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
    }
}
