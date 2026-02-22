using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;

namespace Rememory.Models
{
    public partial class OwnerModel(string path) : ObservableObject
    {
        public int Id { get; set; }

        public string Path { get; set; } = path;

        public string? Name
        {
            get;
            set => SetProperty(ref field, value);
        }

        public byte[]? Icon
        {
            get;
            set {
                if (field != value)
                {
                    field = value;

                    if (App.Current.DispatcherQueue.HasThreadAccess)
                    {
                        // Already on UI thread, run directly
                        UpdateIconBitmap(Icon);
                    }
                    else
                    {
                        // Not on UI thread, enqueue
                        App.Current.DispatcherQueue.TryEnqueue(() => UpdateIconBitmap(Icon));
                    }
                }
            }
        }

        public SoftwareBitmapSource? IconBitmap
        {
            get;
            private set => SetProperty(ref field, value);
        }

        public int ClipsCount { get; set; } = 0;

        private void UpdateIconBitmap(byte[]? icon)
        {
            IconBitmap = icon is null ? null : BitmapHelper.GetBitmapFromBytes(icon);
        }
    }
}
