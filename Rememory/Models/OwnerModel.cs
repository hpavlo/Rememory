using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;

namespace Rememory.Models
{
    public partial class OwnerModel(string path) : ObservableObject
    {
        public int Id { get; set; }

        public string Path { get; set; } = path;

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private byte[]? _icon;
        public byte[]? Icon
        {
            get => _icon;
            set {
                if (_icon != value)
                {
                    _icon = value;
                    IconBitmap = _icon is null ? null : BitmapHelper.GetBitmapFromBytes(_icon);
                }
            }
        }

        private SoftwareBitmapSource? _iconBitmap;
        public SoftwareBitmapSource? IconBitmap
        {
            get => _iconBitmap;
            private set => SetProperty(ref _iconBitmap, value);
        }

        public int ClipsCount { get; set; } = 0;
    }
}
