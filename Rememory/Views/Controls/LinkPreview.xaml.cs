using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Views.Controls.Behavior;
using System;
using System.Runtime.InteropServices;

namespace Rememory.Views.Controls
{
    public sealed partial class LinkPreview : UserControl
    {
        public string LinkUrl { get; private set; }
        public BitmapImage ImageSource { get; private set; } = new();
        public LinkMetadataModel? LinkMetadata { get; private set; }

        public LinkPreview(DataModel dataModel, [Optional] string? searchText)
        {
            DataContext = dataModel;
            LinkUrl = dataModel.Data;
            LinkMetadata = dataModel.Metadata as LinkMetadataModel;
            this.InitializeComponent();

            if (searchText is not null)
            {
                PreviewUrl.SearchHighlight(searchText, LinkUrl);
            }

            if (IsValidUrl(LinkMetadata?.Image, out var uri))
            {
                ImageSource.UriSource = uri;
            }
        }

        private bool IsValidUrl(string? url, out Uri? uri)
        {
            if (string.IsNullOrEmpty(url))
            {
                uri = null;
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private void PreviewImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            PreviewImageBorder.Visibility = Visibility.Visible;
        }
    }
}
