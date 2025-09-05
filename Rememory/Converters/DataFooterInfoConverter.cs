using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
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
                    return "/Clipboard/ClipFooter_CharactersCount/Text".GetLocalizedFormatResource(textData.Data.Length);
                }
                if (DataMap.TryGetValue(ClipboardFormat.Png, out var imageData) || DataMap.TryGetValue(ClipboardFormat.Bitmap, out imageData))
                {
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(imageData.Data);
                        var imageProps = await file.Properties.GetImagePropertiesAsync();
                        return "/Clipboard/ClipFooter_ImageSize/Text".GetLocalizedFormatResource(imageProps.Width, imageProps.Height);
                    }
                    catch { }
                }
                if (DataMap.TryGetValue(ClipboardFormat.Files, out var filesData) && filesData.Metadata is FilesMetadataModel filesMetadata)
                {
                    var footerParts = new List<string>();

                    if (filesMetadata.FilesCount > 0)
                    {
                        footerParts.Add($"{filesMetadata.FilesCount} files");
                    }

                    if (filesMetadata.FoldersCount > 0)
                    {
                        footerParts.Add($"{filesMetadata.FoldersCount} folders");
                    }

                    return string.Join(", ", footerParts);
                }
            }
            return string.Empty;
        }
    }
}
