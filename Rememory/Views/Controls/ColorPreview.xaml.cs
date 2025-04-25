using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rememory.Models;
using Rememory.Views.Controls.Behavior;
using System;
using System.Runtime.InteropServices;

namespace Rememory.Views.Controls
{
    public sealed partial class ColorPreview : UserControl
    {
        // TODO Should we use ARGB or RGBA? Add a setting for that
        public string ColorCode { get; private set; }
        public Brush ColorBrush { get; private set; }
        public string ColorName { get; private set; }

        public ColorPreview(DataModel dataModel, [Optional] string? searchText)
        {
            DataContext = dataModel;
            ColorCode = dataModel.Data;
            var color = ToArgb(ColorCode).ToColor();
            ColorBrush = new SolidColorBrush(color);
            ColorName = Microsoft.UI.ColorHelper.ToDisplayName(color);

            this.InitializeComponent();

            if (searchText is not null)
            {
                ColorCodeTextBlock.SearchHighlight(searchText, ColorCode);
            }
        }

        public string ToArgb(string colorString)
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
