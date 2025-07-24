using Microsoft.UI.Xaml.Data;
using System;

namespace Rememory.Converters
{
    public partial class EnumToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Enum enumValue)
            {
                return System.Convert.ToInt32(enumValue);
            }

            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int index)
            {
                return Enum.ToObject(targetType, index);
            }

            return Enum.GetValues(targetType).GetValue(0)!;
        }
    }
}
