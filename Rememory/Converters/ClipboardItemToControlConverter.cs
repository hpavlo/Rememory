using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Views.Controls;
using System;

namespace Rememory.Converters
{
    public class ClipboardItemToControlConverter : IValueConverter
    {
        /// <summary>
        /// Using to highlight search text in items
        /// </summary>
        public string SearchString { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (ClipboardItem)value;

            foreach (var dataItem in item.DataMap)
            {
                switch (dataItem.Key)
                {
                    case ClipboardFormat.Text:
                        {
                            return item is ClipboardLinkItem ? new LinkPreview(item, SearchString) : new TextPreview(item, SearchString);
                        }
                    case ClipboardFormat.Png:
                        {
                            return new ImagePreview(item);
                        }
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
