using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Rememory.Views.Controls.Behavior
{
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty AutoScrollToStartOnShowProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollToStartOnShow",
                typeof(bool),
                typeof(ScrollViewerBehavior),
                new PropertyMetadata(false, OnAutoScrollToStartOnShowChanged));

        public static bool GetAutoScrollToStartOnShow(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToStartOnShowProperty);
        }

        public static void SetAutoScrollToStartOnShow(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToStartOnShowProperty, value);
        }

        private static void OnAutoScrollToStartOnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer && e.NewValue is bool isEnabled)
            {
                if (isEnabled)
                {
                    scrollViewer.Loaded += ScrollViewer_Loaded;
                }
                else
                {
                    scrollViewer.Loaded -= ScrollViewer_Loaded;
                }
            }
        }

        private static void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ChangeView(0, 0, null, disableAnimation: true);
            }
        }
    }
}
