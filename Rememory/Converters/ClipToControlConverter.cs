using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Views.Controls;
using System;
using System.Collections.Generic;

namespace Rememory.Converters
{
    public partial class ClipToControlConverter : IValueConverter
    {
        /// <summary>
        /// Using to highlight search text in items
        /// </summary>
        public string? SearchString { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var data = (Dictionary<ClipboardFormat, DataModel>)value;

            foreach (var dataItem in data)
            {
                switch (dataItem.Key)
                {
                    case ClipboardFormat.Text:
                        {
                            return dataItem.Value.Metadata switch
                            {
                                LinkMetadataModel => new LinkPreview(dataItem.Value, SearchString),
                                ColorMetadataModel => new ColorPreview(dataItem.Value, SearchString),
                                _ => new TextPreview(dataItem.Value, SearchString)
                            };
                        }
                    case ClipboardFormat.Png:
                    case ClipboardFormat.Bitmap:
                        {
                            return new ImagePreview(dataItem.Value);
                        }
                }
            }

            return new EmptyPreview();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
