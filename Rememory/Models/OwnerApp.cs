using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Rememory.Models
{
    public partial class OwnerApp : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _path = string.Empty;
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        private SoftwareBitmapSource _iconBitmap;
        public SoftwareBitmapSource IconBitmap
        {
            get => _iconBitmap;
            set => SetProperty(ref _iconBitmap, value);
        }

        private int _itemsCount = 0;
        public int ItemsCount
        {
            get => _itemsCount;
            set => SetProperty(ref _itemsCount, value);
        }
    }
}
