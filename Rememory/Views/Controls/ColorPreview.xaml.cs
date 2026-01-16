using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rememory.Models;
using Rememory.Views.Controls.Behavior;
using System;

namespace Rememory.Views.Controls
{
    public sealed partial class ColorPreview : UserControl
    {
        public DataModel ClipData
        {
            get => (DataModel)GetValue(ClipDataProperty);
            set => SetValue(ClipDataProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty ClipDataProperty =
            DependencyProperty.Register(nameof(ClipData), typeof(DataModel), typeof(ColorPreview), new PropertyMetadata(null, OnClipDataChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ColorPreview), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public ColorPreview()
        {
            InitializeComponent();
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPreview control && e.NewValue is DataModel clipData)
            {
                var color = ToArgb(control.ClipData.Data).ToColor();
                control.ColorPreviewBorder.Background = new SolidColorBrush(color);
                control.ColorNameTextBlock.Text = Microsoft.UI.ColorHelper.ToDisplayName(color);
                control.ColorCodeTextBlock.SearchHighlight(clipData.Data);
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPreview control && e.NewValue is string searchText)
            {
                control.ColorCodeTextBlock.SearchHighlight(searchText);
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            string visualState = App.Current.SettingsContext.IsCompactViewEnabled && !PreviewControlsHelper.IsOpenInToolTip(this) ? "CompactView" : "NormalView";
            VisualStateManager.GoToState(this, visualState, true);
        }

        public static string ToArgb(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
            {
                throw new ArgumentException("The parameter \"colorString\" must not be null or empty.");
            }

            colorString = colorString.Trim().ToUpperInvariant();

            if (colorString.StartsWith('#'))
            {
                colorString = colorString.Substring(1);
            }

            string alpha = "FF";
            string red;
            string green;
            string blue;

            switch (colorString.Length)
            {
                case 3: // RGB (short form)
                    red = $"{colorString[0]}{colorString[0]}";
                    green = $"{colorString[1]}{colorString[1]}";
                    blue = $"{colorString[2]}{colorString[2]}";
                    break;
                case 4: // RGBA (short form)
                    red = $"{colorString[0]}{colorString[0]}";
                    green = $"{colorString[1]}{colorString[1]}";
                    blue = $"{colorString[2]}{colorString[2]}";
                    alpha = $"{colorString[3]}{colorString[3]}";
                    break;
                case 6: // RGB (long form)
                    red = colorString.Substring(0, 2);
                    green = colorString.Substring(2, 2);
                    blue = colorString.Substring(4, 2);
                    break;
                case 8: // RGBA (long form)
                    red = colorString.Substring(0, 2);
                    green = colorString.Substring(2, 2);
                    blue = colorString.Substring(4, 2);
                    alpha = colorString.Substring(6, 2);
                    break;
                default:
                    // Invalid format
                    throw new FormatException("The parameter \"colorString\" is not a recognized color format.");
            }

            return $"#{alpha}{red}{green}{blue}";
        }
    }
}
