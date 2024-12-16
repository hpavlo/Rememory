using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Rememory.Views.Controls.Behavior
{
    public static class ImageAutoResizeBehavior
    {
        public static readonly DependencyProperty AutoResizeProperty =
            DependencyProperty.RegisterAttached(
                "AutoResize",
                typeof(bool),
                typeof(ImageAutoResizeBehavior),
                new PropertyMetadata(false, OnAutoResizeChanged));

        public static readonly DependencyProperty MaxImageWidthProperty =
            DependencyProperty.RegisterAttached(
                "MaxImageWidth",
                typeof(double),
                typeof(ImageAutoResizeBehavior),
                new PropertyMetadata(420.0));

        public static readonly DependencyProperty MaxImageHeightProperty =
            DependencyProperty.RegisterAttached(
                "MaxImageHeight",
                typeof(double),
                typeof(ImageAutoResizeBehavior),
                new PropertyMetadata(720.0));

        public static bool GetAutoResize(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoResizeProperty);
        }

        public static void SetAutoResize(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoResizeProperty, value);
        }

        public static double GetMaxImageWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(MaxImageWidthProperty);
        }

        public static void SetMaxImageWidth(DependencyObject obj, double value)
        {
            obj.SetValue(MaxImageWidthProperty, value);
        }

        public static double GetMaxImageHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(MaxImageHeightProperty);
        }

        public static void SetMaxImageHeight(DependencyObject obj, double value)
        {
            obj.SetValue(MaxImageHeightProperty, value);
        }

        private static void OnAutoResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image && e.NewValue is bool isEnabled)
            {
                if (isEnabled)
                {
                    image.ImageOpened += Image_ImageOpened;
                }
                else
                {
                    image.ImageOpened -= Image_ImageOpened;
                }
            }
        }

        private static void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            if (sender is Image image && image.Source is BitmapImage bitmapImage)
            {
                var maxWidth = GetMaxImageWidth(image);
                var maxHeight = GetMaxImageHeight(image);

                if (bitmapImage.PixelWidth <= maxWidth && bitmapImage.PixelHeight <= maxHeight)
                {
                    image.MaxWidth = bitmapImage.PixelWidth;
                    image.MaxHeight = bitmapImage.PixelHeight;
                }
                else
                {
                    image.MaxWidth = maxWidth;
                    image.MaxHeight = maxHeight;
                }
            }
        }
    }
}
