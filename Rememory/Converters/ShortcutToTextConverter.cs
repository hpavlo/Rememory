using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using System;
using System.Collections.Generic;

namespace Rememory.Converters
{
    public partial class ShortcutToTextConverter : IValueConverter
    {
        private readonly string SHORTCUT_SEPARATOR = "+";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<int> keys)
            {
                return KeyboardHelper.ShortcutToString(keys, SHORTCUT_SEPARATOR);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
