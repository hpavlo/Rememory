using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using Rememory.Contracts;
using Rememory.Helper;
using RememoryCore;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

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

            var iconPixels = ProcessInfo.GetProcessIcon(ownerPath)?.ToArray();

            if (iconPixels is null)
            {
                return null;
            }

            return BitmapHelper.GetBitmapFromBytes(iconPixels);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
