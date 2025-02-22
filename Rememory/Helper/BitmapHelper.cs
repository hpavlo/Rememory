using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;

namespace Rememory.Helper
{
    public static class BitmapHelper
    {
        public static SoftwareBitmapSource GetBitmapFromBytes(byte[] pixels)
        {
            if (pixels is null || pixels.Length == 0)
            {
                return null;
            }

            int imageSide = (int)Math.Sqrt(pixels.Length / 4);

            return GetBitmapFromBytes(pixels, imageSide, imageSide);
        }

        public static SoftwareBitmapSource GetBitmapFromBytes(byte[] pixels, int width, int height)
        {
            if (pixels is null || pixels.Length == 0)
            {
                return null;
            }

            var straightBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Straight);
            straightBitmap.CopyFromBuffer(pixels.AsBuffer());
            var premultipliedBitmap = SoftwareBitmap.Convert(straightBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var bitmapSource = new SoftwareBitmapSource();
            bitmapSource.SetBitmapAsync(premultipliedBitmap).AsTask();

            return bitmapSource;
        }
    }
}
