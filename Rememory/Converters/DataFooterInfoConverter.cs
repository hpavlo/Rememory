using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Rememory.Converters
{
    public partial class DataFooterInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new TaskCompletionNotifier<string>(Task.Run(() => GetFooterInfo((Dictionary<ClipboardFormat, DataModel>)value)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GetFooterInfo(Dictionary<ClipboardFormat, DataModel> DataMap)
        {
            if (DataMap is not null)
            {
                if (DataMap.TryGetValue(ClipboardFormat.Text, out var textData))
                {
                    return "CharactersCount".GetLocalizedFormatResource(textData.Data.Length);
                }
                if (DataMap.TryGetValue(ClipboardFormat.Png, out var imageFile) || DataMap.TryGetValue(ClipboardFormat.Bitmap, out imageFile))
                {
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(imageFile.Data);
                        var imageProps = await file.Properties.GetImagePropertiesAsync();
                        return "ImageSize".GetLocalizedFormatResource(imageProps.Width, imageProps.Height);
                    }
                    catch { }
                }
            }
            return string.Empty;
        }
    }
}
