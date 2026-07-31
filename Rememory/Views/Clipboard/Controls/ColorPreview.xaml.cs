using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Rememory.Models;
using System;
using Windows.UI;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class ColorPreview : DataPreviewBase
    {
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(ColorPreview), new PropertyMetadata(Colors.Transparent, OnColorChanged));
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty HexColorProperty =
            DependencyProperty.Register(nameof(HexColor), typeof(string), typeof(ColorPreview), new PropertyMetadata(string.Empty));
        public string HexColor
        {
            get => (string)GetValue(HexColorProperty);
            set => SetValue(HexColorProperty, value);
        }

        public static readonly DependencyProperty RgbColorProperty =
            DependencyProperty.Register(nameof(RgbColor), typeof(string), typeof(ColorPreview), new PropertyMetadata(string.Empty));
        public string RgbColor
        {
            get => (string)GetValue(RgbColorProperty);
            set => SetValue(RgbColorProperty, value);
        }

        public static readonly DependencyProperty RgbaColorProperty =
            DependencyProperty.Register(nameof(RgbaColor), typeof(string), typeof(ColorPreview), new PropertyMetadata(string.Empty));
        public string RgbaColor
        {
            get => (string)GetValue(RgbaColorProperty);
            set => SetValue(RgbaColorProperty, value);
        }

        public static readonly DependencyProperty HslColorProperty =
            DependencyProperty.Register(nameof(HslColor), typeof(string), typeof(ColorPreview), new PropertyMetadata(string.Empty));
        public string HslColor
        {
            get => (string)GetValue(HslColorProperty);
            set => SetValue(HslColorProperty, value);
        }

        public static readonly DependencyProperty HslaColorProperty =
            DependencyProperty.Register(nameof(HslaColor), typeof(string), typeof(ColorPreview), new PropertyMetadata(string.Empty));
        public string HslaColor
        {
            get => (string)GetValue(HslaColorProperty);
            set => SetValue(HslaColorProperty, value);
        }

        public bool IsAllFarmatsAvailable => PreviewVisualState == DataPreviewVisualState.Expanded;

        public ColorPreview()
        {
            InitializeComponent();
        }

        protected override void OnClipDataChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnClipDataChanged(args);

            if (args.NewValue is DataModel clipData)
            {
                Color = Helper.ColorHelper.StringToColor(clipData.Data);
                // Setting textData manually since ColorCodeTextBlock.Text is not updated yet
                SearchHighlight(ColorCodeTextBlock, SearchText, clipData.Data);
            }
        }

        protected override void OnSearchTextChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnSearchTextChanged(args);

            if (args.NewValue is string searchText)
            {
                SearchHighlight(ColorCodeTextBlock, searchText);
            }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPreview colorPreview && e.NewValue is Color color)
            {
                byte a = color.A, r = color.R, g = color.G, b = color.B;

                var hsl = color.ToHsl();
                int h = (int)Math.Round(hsl.H);
                int s = (int)Math.Round(hsl.S * 100);
                int l = (int)Math.Round(hsl.L * 100);

                colorPreview.HexColor = a == 255 ? $"#{r:x2}{g:x2}{b:x2}" : $"#{r:x2}{g:x2}{b:x2}{a:x2}";

                colorPreview.RgbColor = $"rgb({r}, {g}, {b})";
                colorPreview.RgbaColor = $"rgba({r}, {g}, {b}, {Math.Round(a / 255f, 2)})";

                colorPreview.HslColor = $"hsl({h}, {s}%, {l}%)";
                colorPreview.HslaColor = $"hsla({h}, {s}%, {l}%, {Math.Round(hsl.A, 2)})";
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            SearchHighlight(ColorCodeTextBlock, SearchText);
        }
    }
}
