using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Models;
using Rememory.Models.Metadata;
using System;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class LinkPreview : DataPreviewBase
    {
        private const string BaseVisualStateName = "BaseView";
        private const string LinkMetadataNotAvailableVisualStateName = "LinkMetadataNotAvailable";

        public static readonly DependencyProperty LinkMetadataProperty =
            DependencyProperty.Register(nameof(LinkMetadata), typeof(LinkMetadataModel), typeof(LinkPreview), new PropertyMetadata(null));
        public LinkMetadataModel? LinkMetadata
        {
            get => (LinkMetadataModel)GetValue(LinkMetadataProperty);
            set => SetValue(LinkMetadataProperty, value);
        }

        public BitmapImage ImageSource { get; private set; } = new();

        public LinkPreview()
        {
            InitializeComponent();
        }

        protected override void OnClipDataChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnClipDataChanged(args);

            if (args.OldValue is DataModel oldClipData)
            {
                oldClipData.PropertyChanged -= ClipData_PropertyChanged;
            }

            if (args.NewValue is DataModel clipData)
            {
                clipData.PropertyChanged += ClipData_PropertyChanged;

                UpdateMetadata(clipData.Metadata);

                // Setting textData manually since PreviewUrl.Text is not updated yet
                SearchHighlight(PreviewUrl, SearchText, clipData.Data);
            }
            else
            {
                LinkMetadata = null;
                ImageSource.UriSource = null;
            }
        }

        protected override void OnSearchTextChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnSearchTextChanged(args);

            if (args.NewValue is string searchText)
            {
                SearchHighlight(PreviewUrl, searchText);
            }
        }

        protected override void TriggerVisualState(DataPreviewVisualState visualState)
        {
            if (LinkMetadata is null && visualState != DataPreviewVisualState.Compact)
            {
                VisualStateManager.GoToState(this, LinkMetadataNotAvailableVisualStateName, true);
            }
            else
            {
                /// Used to reset visual state since we have PreviewGrid
                /// that is Loaded only LinkMetadata is available
                /// and after LinkMetadata is changed we should trigger state again to update grid visual
                VisualStateManager.GoToState(this, BaseVisualStateName, true);
                base.TriggerVisualState(visualState);
            }
        }

        private void UpdateMetadata(IMetadata? metadata)
        {
            if (PreviewVisualState == DataPreviewVisualState.Compact)
            {
                TriggerVisualState(PreviewVisualState);
                return;
            }

            LinkMetadata = metadata is LinkMetadataModel { Title: not null, Description: not null } linkMetadata ? linkMetadata : null;

            TriggerVisualState(PreviewVisualState);

            // Clear old values to trigger image source update
            ImageSource.UriSource = null;
            PreviewImageBorder?.Visibility = Visibility.Collapsed;

            if (IsValidUrl(LinkMetadata?.Image, out var uri))
            {
                ImageSource.UriSource = uri;
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            /// Used if ClipData is not changed but the control is reused.
            /// In this case we just trigger metadata and visual state.
            if (PreviewVisualState == DataPreviewVisualState.Compact)
            {
                // Set LinkMetadata to null to hide link preview
                UpdateMetadata(null);
            }
            else if (LinkMetadata is null && ClipData?.Metadata is not null)
            {
                UpdateMetadata(ClipData.Metadata);
            }

            SearchHighlight(PreviewUrl, SearchText);
        }

        private void ParentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ClipData?.PropertyChanged -= ClipData_PropertyChanged;
        }

        private async void PreviewImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            if (PreviewGrid is null)
            {
                return;
            }

            // Show image only if it opened successfully
            PreviewImageBorder.Visibility = Visibility.Visible;

            if (PreviewVisualState != DataPreviewVisualState.Expanded)
            {
                return;
            }

            /// For Expanded view only.
            /// Adjust the preview image to square if it fits

            PreviewImageBorder.MaxHeight = PreviewGrid.ActualWidth;

            if (ImageSource.PixelWidth < PreviewGrid.ActualWidth && ImageSource.PixelHeight < PreviewImageBorder.MaxHeight)
            {
                PreviewImage.Stretch = Stretch.None;
            }
            else
            {
                PreviewImage.Stretch = Stretch.Uniform;
            }
        }

        private void ClipData_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClipData.Metadata))
            {
                UpdateMetadata(ClipData?.Metadata);
            }
        }

        private static bool IsValidUrl(string? url, out Uri? uri)
        {
            if (string.IsNullOrEmpty(url))
            {
                uri = null;
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
