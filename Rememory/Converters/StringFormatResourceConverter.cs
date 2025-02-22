﻿using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using System;

namespace Rememory.Converters
{
    public partial class StringFormatResourceConverter : StringFormatConverter, IValueConverter
    {
        public new object Convert(object value, Type targetType, object parameter, string language)
        {
            string resourceName = parameter as string;

            return base.Convert(value, targetType, resourceName.GetLocalizedResource(), language);
        }
    }
}
