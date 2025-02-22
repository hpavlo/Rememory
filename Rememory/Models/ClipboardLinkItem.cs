using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using System.Collections.Generic;

namespace Rememory.Models
{
    public partial class ClipboardLinkItem : ClipboardItem
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private BitmapImage _image = new();
        public BitmapImage Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        public string LinkValue => DataMap.GetValueOrDefault(ClipboardFormat.Text);

        private bool _hasInfoLoaded = false;
        public bool HasInfoLoaded
        {
            get => _hasInfoLoaded;
            set => SetProperty(ref _hasInfoLoaded, value);
        }

        public ClipboardLinkItem() { }

        public ClipboardLinkItem(ClipboardItem clipboardItem) : base(clipboardItem) { }
    }
}
