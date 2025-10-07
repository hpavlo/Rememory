using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Views.Controls.Behavior;
using System;

namespace Rememory.Views.Controls
{
    public sealed partial class LinkPreview : UserControl
    {
        private bool? _showInCompactMode;

        public DataModel ClipData
        {
            get => (DataModel)GetValue(ClipDataProperty);
            set => SetValue(ClipDataProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public LinkMetadataModel? LinkMetadata
        {
            get => (LinkMetadataModel)GetValue(LinkMetadataProperty);
            set => SetValue(LinkMetadataProperty, value);
        }

        public BitmapImage ImageSource { get; private set; } = new();

        public static readonly DependencyProperty ClipDataProperty =
            DependencyProperty.Register(nameof(ClipData), typeof(DataModel), typeof(LinkPreview), new PropertyMetadata(null, OnClipDataChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(LinkPreview), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public static readonly DependencyProperty LinkMetadataProperty =
            DependencyProperty.Register(nameof(LinkMetadata), typeof(LinkMetadataModel), typeof(LinkPreview), new PropertyMetadata(null));

        public LinkPreview()
        {
            InitializeComponent();
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinkPreview control)
            {
                if (e.OldValue is DataModel oldClipData)
                {
                    oldClipData.PropertyChanged -= control.ClipData_PropertyChanged;
                }
                if (e.NewValue is DataModel clipData)
                {
                    clipData.PropertyChanged += control.ClipData_PropertyChanged;

                    control.UpdateMetadata(clipData.Metadata);
                }
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinkPreview control && e.NewValue is string searchText)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    control.PreviewUrl.TextHighlighters.Clear();
                }
                else
                {
                    control.PreviewUrl.SearchHighlight(searchText);
                }
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            _showInCompactMode = App.Current.SettingsContext.IsCompactViewEnabled && !PreviewControlsHelper.IsOpenInToolTip(this);

            if (_showInCompactMode is true)
            {
                VisualStateManager.GoToState(this, "CompactView", true);
                LinkMetadata = null;   // To hide metadata in compact view
            }
            else if (LinkMetadata is null)
            {
                UpdateMetadata(ClipData.Metadata);
            }
        }

        private void ParentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _showInCompactMode = null;

            if (ClipData != null)
            {
                ClipData.PropertyChanged -= ClipData_PropertyChanged;
            }
        }

        private void PreviewImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            if (PreviewImageBorder is not null)
            {
                PreviewImageBorder.Visibility = Visibility.Visible;
            }
        }

        private void ClipData_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClipData.Metadata))
            {
                UpdateMetadata(ClipData.Metadata);
            }
        }

        private void UpdateMetadata(IMetadata? metadata)
        {
            LinkMetadata = metadata as LinkMetadataModel;

            string visualState = _showInCompactMode is true
                ? "CompactView"
                : LinkMetadata is null
                    ? "LinkMetadataNotAvailable"
                    : "NormalView";

            VisualStateManager.GoToState(this, visualState, true);

            ImageSource.UriSource = IsValidUrl(LinkMetadata?.Image, out var uri) ? uri : null;

            PreviewImageBorder?.SetValue(VisibilityProperty, Visibility.Collapsed);
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
