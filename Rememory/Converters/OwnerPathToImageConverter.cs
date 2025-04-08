using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using Rememory.Contracts;
using Rememory.Helper;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Rememory.Converters
{
    public partial class OwnerPathToImageConverter : IValueConverter
    {
        private readonly IOwnerService _ownerAppService = App.Current.Services.GetService<IOwnerService>()!;

        public unsafe object? Convert(object value, Type targetType, object parameter, string language)
        {
            var ownerPath = (string)value;

            // Try to get cached bitmap value form owner app dictionary
            if (_ownerAppService.Owners.TryGetValue(ownerPath, out var owner) && owner.IconBitmap is not null)
            {
                return owner.IconBitmap;
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
