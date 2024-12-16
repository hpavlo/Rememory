using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Rememory.Converters
{
    public class DataFooterInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new TaskCompletionNotifier<string>(GetFooterInfo((Dictionary<ClipboardFormat, string>)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GetFooterInfo(Dictionary<ClipboardFormat, string> DataMap)
        {
            if (DataMap is not null)
            {
                if (DataMap.TryGetValue(ClipboardFormat.Text, out string textData))
                {
                    return "CharactersCount".GetLocalizedFormatResource(textData.Length);
                }
                if (DataMap.TryGetValue(ClipboardFormat.Png, out string pngFile))
                {
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(pngFile);
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
