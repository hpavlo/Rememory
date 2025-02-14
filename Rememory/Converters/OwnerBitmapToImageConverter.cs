using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;

namespace Rememory.Converters
{
    public class OwnerBitmapToImageConverter : IValueConverter
    {
        // Saves cached bitmaps for each application to make them reusable
        private static Dictionary<string, SoftwareBitmapSource> _cachedBitmaps = [];

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (ClipboardItem)value;

            if (_cachedBitmaps.TryGetValue(item.OwnerPath, out var cachedBitmap))
            {
                return cachedBitmap;
            }

            if (item.OwnerIconBitmap is not null)
            {
                int imageSide = (int)Math.Sqrt(item.OwnerIconBitmap.Length / 4);
                var straightBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, imageSide, imageSide, BitmapAlphaMode.Straight);
                straightBitmap.CopyFromBuffer(item.OwnerIconBitmap.AsBuffer());
                var premultipliedBitmap = SoftwareBitmap.Convert(straightBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                var bitmapSource = new SoftwareBitmapSource();
                bitmapSource.SetBitmapAsync(premultipliedBitmap).AsTask();

                _cachedBitmaps.Add(item.OwnerPath, bitmapSource);
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
