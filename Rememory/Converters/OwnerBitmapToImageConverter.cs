using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
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

                var straightBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, imageSide, imageSide, BitmapAlphaMode.Straight);
                straightBitmap.CopyFromBuffer(pixels.AsBuffer());
                var premultipliedBitmap = SoftwareBitmap.Convert(straightBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                var bitmapSource = new SoftwareBitmapSource();
                bitmapSource.SetBitmapAsync(premultipliedBitmap).AsTask();
                return bitmapSource;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
