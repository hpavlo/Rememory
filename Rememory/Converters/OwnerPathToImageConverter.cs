using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using Rememory.Service;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Rememory.Converters
{
    public class OwnerPathToImageConverter : IValueConverter
    {
        private readonly IOwnerAppService _ownerAppService = App.Current.Services.GetService<IOwnerAppService>();

        public unsafe object Convert(object value, Type targetType, object parameter, string language)
        {
            var ownerPath = (string)value;

            // Try to get cached bitmap value form owner app dictionary
            if (_ownerAppService.GetOwnerBitmap(ownerPath) is SoftwareBitmapSource cachedBitmap)
            {
                return cachedBitmap;
            }

            if (!Path.Exists(ownerPath))
            {
                return null;
            }

            var pathPointer = Utf16StringMarshaller.ConvertToUnmanaged(ownerPath);
            int length = 0;
            IntPtr pixelsPointer = IntPtr.Zero;

            RememoryCoreHelper.GetOwnerIcon((IntPtr)pathPointer, ref length, ref pixelsPointer);

            if (length == 0 || pixelsPointer == IntPtr.Zero)
            {
                return null;
            }

            byte[] pixels = new byte[length];
            Marshal.Copy(pixelsPointer, pixels, 0, length);

            // Free all resources
            Utf16StringMarshaller.Free(pathPointer);
            if (pixelsPointer != IntPtr.Zero)
            {
                RememoryCoreHelper.FreeOwnerIcon(ref pixelsPointer);
            }

            return BitmapHelper.GetBitmapFromBytes(pixels);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
