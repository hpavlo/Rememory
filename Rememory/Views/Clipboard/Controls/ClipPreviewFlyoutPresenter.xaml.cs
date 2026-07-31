using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Core;
using Rememory.Models;
using Rememory.Models.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class ClipPreviewFlyoutPresenter : FlyoutPresenter
    {
        private const int MaxFlyoutWidth = 800;
        private readonly List<PreviewMapping> _previewMaps;
        private readonly List<SegmentedItem> _segmentedItems;

        public static readonly DependencyProperty ClipModelProperty =
            DependencyProperty.Register(nameof(ClipModel), typeof(DataModel), typeof(ClipPreviewFlyoutPresenter), new PropertyMetadata(null, OnClipModelChanged));

        public ClipModel? ClipModel
        {
            get => (ClipModel)GetValue(ClipModelProperty);
            set => SetValue(ClipModelProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ClipPreviewFlyoutPresenter), new PropertyMetadata(string.Empty));
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public Size TargetSize { get; set; }

        public ClipPreviewFlyoutPresenter()
        {
            InitializeComponent();

            _previewMaps = GetPreviewItemMaps();
            _segmentedItems = [.. FormatSegmentedSelector.Items.OfType<SegmentedItem>()];
        }

        private static void OnClipModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ClipPreviewFlyoutPresenter presenter)
            {
                return;
            }

            presenter.ResetScrollViewer();
            presenter.FormatSegmentedSelector.SelectedItem = null;

            bool isFirstTabIsSelected = false;

            /// Collapse items in this cycle only
            /// to avoid collapsing shared segmented items
            /// between preview controls (e.g. TextSegmentedItem)
            foreach (var map in presenter._previewMaps)
            {
                map.SegmentedItem.IsSelected = false;
                map.SegmentedItem.Visibility = Visibility.Collapsed;
            }

            foreach (var map in presenter._previewMaps)
            {
                if (e.NewValue is not ClipModel clip || !map.IsSupported(clip))
                {
                    continue;
                }

                map.SegmentedItem.Visibility = Visibility.Visible;

                if (!isFirstTabIsSelected)
                {
                    isFirstTabIsSelected = true;
                    map.SegmentedItem.IsSelected = true;
                }
            }

            presenter.TriggerSegmentedSelectorVisibility();
        }

        public void ShowNextFormatPreview()
        {
            int currentIndex = _segmentedItems.FindIndex(item => item.IsSelected);
            if (currentIndex < 0 || currentIndex >= _segmentedItems.Count - 1)
            {
                return;
            }

            int nexIndex = _segmentedItems.FindIndex(currentIndex + 1, item => item.Visibility == Visibility.Visible);

            if (nexIndex != -1)
            {
                _segmentedItems[currentIndex].IsSelected = false;
                _segmentedItems[nexIndex].IsSelected = true;
            }
        }

        public void ShowPreviousFormatPreview()
        {
            int currentIndex = _segmentedItems.FindIndex(item => item.IsSelected);
            if (currentIndex <= 0)
            {
                return;
            }

            int prevIndex = _segmentedItems.FindLastIndex(currentIndex - 1, item => item.Visibility == Visibility.Visible);

            if (prevIndex != -1)
            {
                _segmentedItems[currentIndex].IsSelected = false;
                _segmentedItems[prevIndex].IsSelected = true;
            }
        }

        private void FlyoutPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            if (_segmentedItems.All(item => !item.IsSelected))
            {
                // Manually select item on first flyout load
                var itemToSelect = _segmentedItems.FirstOrDefault(item => item.Visibility == Visibility.Visible);
                itemToSelect?.IsSelected = true;
            }

            /// Trigger size update after flyout is loaded
            /// to force use actual flyout size, based on the selected tab
            var selectedItem = FormatSegmentedSelector.SelectedItem as SegmentedItem;
            UpdateFlyoutSize(selectedItem);
        }

        private void FlyoutPresenter_Unloaded(object sender, RoutedEventArgs e)
        {
            ResetScrollViewer();

            // Trigger SelectedItem to clear all preview controls data 
            FormatSegmentedSelector.SelectedItem = null;
        }

        private void FormatSegmentedSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetScrollViewer();
            var selectedItem = FormatSegmentedSelector.SelectedItem as SegmentedItem;            

            foreach (var map in _previewMaps)
            {
                if (map.SegmentedItem == selectedItem && ClipModel is not null && map.IsSupported(ClipModel))
                {
                    UpdateFlyoutSize(selectedItem);

                    // Html preview has own scroll viewer
                    TriggerScrollViewVisibility(selectedItem != HtmlSegmentedItem);

                    map.PopulateData(ClipModel);
                    map.PreviewControl.Visibility = Visibility.Visible;
                }
                else
                {
                    map.PreviewControl.Visibility = Visibility.Collapsed;
                    map.PreviewControl.ClipData = null;
                }
            }
        }

        private void ResetScrollViewer()
        {
            DataPreviewScrollView.ZoomTo(1, new(0, 0), new ScrollingZoomOptions(ScrollingAnimationMode.Disabled));
            DataPreviewScrollView.ScrollTo(0, 0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
        }

        /// <summary>
        /// Sets the flyout maximum width and height based on the selected format.
        /// For image and html preview width is multiplied on 1.5, and MinWidth is set to the same value to force the maximum view.
        /// MaxWidth is constrained by <see cref="MaxFlyoutWidth"/>
        /// </summary>
        private void UpdateFlyoutSize(SegmentedItem? selectedItem)
        {
            if (selectedItem == ImageSegmentedItem || selectedItem == HtmlSegmentedItem)
            {
                var width = Math.Min(TargetSize.Width * 1.5, MaxFlyoutWidth);
                MinWidth = width;
                MaxWidth = width;
            }
            else
            {
                MinWidth = SettingsContext.WindowWidthLowerBound;
                MaxWidth = Math.Min(TargetSize.Width, MaxFlyoutWidth);
            }

            MaxHeight = TargetSize.Height;
        }

        private void TriggerScrollViewVisibility(bool setVisible)
        {
            if (setVisible)
            {
                DataPreviewScrollView.Visibility = Visibility.Visible;
                MinHeight = 40;   // Default MinHeight for flyout presenter
            }
            else
            {
                DataPreviewScrollView.Visibility = Visibility.Collapsed;
                // Set MinHeight instead of Height to force the flyout to be full height
                MinHeight = MaxHeight;
            }
        }

        private void TriggerSegmentedSelectorVisibility()
        {
            // Keep FormatSegmentedSelector visible to allow select SegmentedItems
            if (_segmentedItems.Count(item => item.Visibility == Visibility.Visible) == 1)
            {
                FormatSegmentedSelector.Height = 0;
                FormatSegmentedSelector.Opacity = 0;
                FormatSegmentedSelector.Margin = new(0);
            }
            else
            {
                FormatSegmentedSelector.Height = double.NaN;
                FormatSegmentedSelector.Opacity = 1;
                FormatSegmentedSelector.Margin = new(0, 0, 0, 14);
            }
        }

        private List<PreviewMapping> GetPreviewItemMaps()
        {
            return [
                new(ImageSegmentedItem,
                    ImagePreview,
                    clip => clip.Data.ContainsKey(ClipboardFormat.Png) || clip.Data.ContainsKey(ClipboardFormat.Bitmap),
                    clip => ImagePreview.ClipData = clip.Data.TryGetValue(ClipboardFormat.Png, out var dataModel) ? dataModel : clip.Data[ClipboardFormat.Bitmap]),
                new(FilesSegmentedItem,
                    FilesPreview,
                    clip => clip.Data.ContainsKey(ClipboardFormat.Files),
                    clip => FilesPreview.ClipData = clip.Data[ClipboardFormat.Files]),
                new(TextSegmentedItem,
                    TextPreview,
                    clip => clip.Data.TryGetValue(ClipboardFormat.Text, out var dataModel) && !clip.IsLink && dataModel.Metadata is not ColorMetadataModel,
                    clip => TextPreview.ClipData = clip.Data[ClipboardFormat.Text]),
                new(TextSegmentedItem,
                    LinkPreview,
                    clip => clip.Data.ContainsKey(ClipboardFormat.Text) && clip.IsLink,
                    clip => LinkPreview.ClipData = clip.Data[ClipboardFormat.Text]),
                new(TextSegmentedItem,
                    ColorPreview,
                    clip => clip.Data.TryGetValue(ClipboardFormat.Text, out var dataModel) && dataModel.Metadata is ColorMetadataModel,
                    clip => ColorPreview.ClipData = clip.Data[ClipboardFormat.Text]),
                new(RtfSegmentedItem,
                    RichTextFormatPreview,
                    clip => clip.Data.ContainsKey(ClipboardFormat.Rtf),
                    clip => RichTextFormatPreview.ClipData = clip.Data[ClipboardFormat.Rtf]),
                new(HtmlSegmentedItem,
                    HtmlPreview,
                    clip => clip.Data.ContainsKey(ClipboardFormat.Html),
                    clip => HtmlPreview.ClipData = clip.Data[ClipboardFormat.Html])
            ];
        }

        private record PreviewMapping(SegmentedItem SegmentedItem, DataPreviewBase PreviewControl, Func<ClipModel, bool> IsSupported, Action<ClipModel> PopulateData);
    }
}
