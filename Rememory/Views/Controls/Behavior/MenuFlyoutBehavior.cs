using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Rememory.Views.Controls.Behavior
{
    public static class MenuFlyoutBehavior
    {
        public static readonly DependencyProperty AutoDisableProperty =
        DependencyProperty.RegisterAttached(
            "AutoDisable",
            typeof(bool),
            typeof(MenuFlyoutBehavior),
            new PropertyMetadata(false, OnAutoDisableChanged));

        public static void SetAutoDisable(DependencyObject element, bool value)
        {
            element.SetValue(AutoDisableProperty, value);
        }

        public static bool GetAutoDisable(DependencyObject element)
        {
            return (bool)element.GetValue(AutoDisableProperty);
        }

        private static void OnAutoDisableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MenuFlyoutSubItem subItem)
            {
                if ((bool)e.NewValue)
                {
                    subItem.Loaded += SubItem_Loaded;
                }
                else
                {
                    subItem.Unloaded -= SubItem_Loaded;
                }
            }
        }

        private static void SubItem_Loaded(object sender, RoutedEventArgs e)
        {
            var subItem = (MenuFlyoutSubItem)sender;
            bool anyEnabled = subItem.Items.OfType<MenuFlyoutItem>().Any(item => item.Command.CanExecute(subItem.DataContext));
            subItem.IsEnabled = anyEnabled;
        }
    }
}
