using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Rememory.Converters
{
    public class OwnerBitmapToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not null)
            {
                var pixels = (byte[])value;
                int imageSide = (int)Math.Sqrt(pixels.Length / 4);
                return new TaskCompletionNotifier<SoftwareBitmapSource>(CreateBitmap(imageSide, imageSide, pixels));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private async Task<SoftwareBitmapSource> CreateBitmap(int width, int height, byte[] pixels)
        {
            var straightBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Straight);
            straightBitmap.CopyFromBuffer(pixels.AsBuffer());
            var premultipliedBitmap = SoftwareBitmap.Convert(straightBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(premultipliedBitmap);
            return bitmapSource;
        }
    }
}
