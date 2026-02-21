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

                    if (App.Current.DispatcherQueue.HasThreadAccess)
                    {
                        // Already on UI thread, run directly
                        UpdateIconBitmap(_icon);
                    }
                    else
                    {
                        // Not on UI thread, enqueue
                        App.Current.DispatcherQueue.TryEnqueue(() => UpdateIconBitmap(_icon));
                    }
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

        private void UpdateIconBitmap(byte[]? icon)
        {
            IconBitmap = icon is null ? null : BitmapHelper.GetBitmapFromBytes(icon);
        }
    }
}
