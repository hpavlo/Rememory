using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Rememory.ViewModels;
using System;

namespace Rememory.Converters
{
    public class NavigationSelectedMenuItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var selectorBar = (SelectorBar)parameter;
            var selectedItem = (ClipboardRootPageViewModel.NavigationMenuItem)value;
            return selectorBar.Items[(int)selectedItem];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var selectorBar = (SelectorBar)parameter;
            var selectorBarItem = (SelectorBarItem)value;
            return selectorBar.Items.IndexOf(selectorBarItem);
        }
    }
}
