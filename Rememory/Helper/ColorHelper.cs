using Microsoft.UI;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Windows.UI;

namespace Rememory.Helper
{
    public static partial class ColorHelper 
    {
        public static readonly Regex HexColorRegex_ = HexColorRegex();
        public static readonly Regex HexColorOptionalPrefixRegex_ = HexColorOptionalPrefixRegex();
        public static readonly Regex RgbColorRegex_ = RgbColorRegex();
        public static readonly Regex HslColorRegex_ = HslColorRegex();

        /// <summary>
        /// Checks Hex, RGB or Hsl formats (with alpha channel)
        /// </summary>
        /// <returns>True if string is a color, otherwise False</returns>
        public static bool IsValidColor(this string str, bool isHexColorPrefixRequired)
        {
            bool isHexMatch = (isHexColorPrefixRequired ? HexColorRegex_ : HexColorOptionalPrefixRegex_).IsMatch(str);
            bool isRgbMatch = RgbColorRegex_.IsMatch(str);
            bool isHslMatch = HslColorRegex_.IsMatch(str);

            return isHexMatch || isRgbMatch || isHslMatch;
        }

        /// <summary>
        /// Converts HEX, RGB, RGBA, HSL, HSLA color formats to <see cref="Windows.UI.Color"/>
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        public static Color StringToColor(string colorString)
        {
            if (string.IsNullOrWhiteSpace(colorString))
            {
                return Colors.Transparent;
            }

            try
            {
                var colorSpan = colorString.AsSpan().Trim();

                // Check for RGB / RGBA
                if (colorSpan.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    var match = RgbColorRegex().Match(colorString);
                    if (match.Success)
                    {
                        byte r = ParseRgbComponent(match.Groups[1].ValueSpan);
                        byte g = ParseRgbComponent(match.Groups[2].ValueSpan);
                        byte b = ParseRgbComponent(match.Groups[3].ValueSpan);
                        byte a = match.Groups[4].Success ? ParseAlphaComponent(match.Groups[4].ValueSpan) : (byte)255;

                        return Color.FromArgb(a, r, g, b);
                    }
                }

                // Check for HSL / HSLA
                if (colorSpan.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
                {
                    var match = HslColorRegex().Match(colorString);
                    if (match.Success)
                    {
                        float hue = Math.Clamp(float.Parse(match.Groups[1].ValueSpan, CultureInfo.InvariantCulture) % 360f, 0f, 360f);
                        float saturation = Math.Clamp(float.Parse(match.Groups[2].ValueSpan, CultureInfo.InvariantCulture), 0f, 100f) / 100f;
                        float lightness = Math.Clamp(float.Parse(match.Groups[3].ValueSpan, CultureInfo.InvariantCulture), 0f, 100f) / 100f;
                        byte a = match.Groups[4].Success ? ParseAlphaComponent(match.Groups[4].ValueSpan) : (byte)255;

                        return CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsl(hue, saturation, lightness, a / 255.0);
                    }
                }

                // Fallback for HEX format
                return RgbaToColor(colorSpan.ToString());
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                return Colors.Transparent;
            }
        }

        private static byte ParseRgbComponent(ReadOnlySpan<char> input)
        {
            input = input.Trim();
            if (input.EndsWith('%'))
            {
                float val = float.Parse(input[..^1], CultureInfo.InvariantCulture);
                return (byte)Math.Round(255f / 100f * Math.Clamp(val, 0f, 100f));
            }

            float rawVal = float.Parse(input, CultureInfo.InvariantCulture);
            return (byte)Math.Round(Math.Clamp(rawVal, 0f, 255f));
        }

        private static byte ParseAlphaComponent(ReadOnlySpan<char> input)
        {
            input = input.Trim();
            if (input.IsEmpty)
            {
                return 255;
            }

            if (input.EndsWith('%'))
            {
                float val = float.Parse(input[..^1], CultureInfo.InvariantCulture);
                return (byte)Math.Round(255f / 100f * Math.Clamp(val, 0f, 100f));
            }

            /// If alpha is provided as a byte value (> 1, up to 255), clamp and return directly.
            /// Otherwise, treat it as a normalized decimal (0.0 to 1.0) and scale to a 0–255 byte range.
            float alphaValue = float.Parse(input, CultureInfo.InvariantCulture);
            if (alphaValue > 1f)
            {
                return (byte)Math.Round(Math.Clamp(alphaValue, 0f, 255f));
            }

            return (byte)Math.Round(255f * Math.Clamp(alphaValue, 0f, 1f));
        }

        /// <summary>
        /// WinUI colors work with ARGB format but web apps uses RGBA.
        /// This converter used only to crete <see cref="Windows.UI.Color"/> and preview it to user
        /// </summary>
        private static Color RgbaToColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
            {
                throw new ArgumentException("The parameter \"colorString\" must not be null or empty.");
            }

            ReadOnlySpan<char> hex = colorString.AsSpan().TrimStart('#').Trim();

            byte r, g, b, a = 255;

            switch (hex.Length)
            {
                case 3: // #RGB -> duplicate chars
                    r = Convert.ToByte(new string(hex[0], 2), 16);
                    g = Convert.ToByte(new string(hex[1], 2), 16);
                    b = Convert.ToByte(new string(hex[2], 2), 16);
                    break;

                case 4: // #RGBA -> duplicate chars
                    r = Convert.ToByte(new string(hex[0], 2), 16);
                    g = Convert.ToByte(new string(hex[1], 2), 16);
                    b = Convert.ToByte(new string(hex[2], 2), 16);
                    a = Convert.ToByte(new string(hex[3], 2), 16);
                    break;

                case 6: // #RRGGBB
                    r = Convert.ToByte(hex.Slice(0, 2).ToString(), 16);
                    g = Convert.ToByte(hex.Slice(2, 2).ToString(), 16);
                    b = Convert.ToByte(hex.Slice(4, 2).ToString(), 16);
                    break;

                case 8: // #RRGGBBAA
                    r = Convert.ToByte(hex.Slice(0, 2).ToString(), 16);
                    g = Convert.ToByte(hex.Slice(2, 2).ToString(), 16);
                    b = Convert.ToByte(hex.Slice(4, 2).ToString(), 16);
                    a = Convert.ToByte(hex.Slice(6, 2).ToString(), 16);
                    break;

                default:
                    throw new FormatException($"The color string '{colorString}' is not a recognized hex color format.");
            }

            return Color.FromArgb(a, r, g, b);
        }

        [GeneratedRegex(@"^(?i)\s*#([a-f0-9]{8}|[a-f0-9]{6}|[a-f0-9]{4}|[a-f0-9]{3})\s*$")]
        private static partial Regex HexColorRegex();

        [GeneratedRegex(@"^(?i)\s*#?([a-f0-9]{8}|[a-f0-9]{6}|[a-f0-9]{4}|[a-f0-9]{3})\s*$")]
        private static partial Regex HexColorOptionalPrefixRegex();

        [GeneratedRegex(@"^(?i)\s*rgba?\(\s*(\d+\.\d+%?|\d+%?)\s*[\s,]+(\d+\.\d+%?|\d+%?)\s*[\s,]+(\d+\.\d+%?|\d+%?)(?:\s*[\s,/]\s*(\d+\.\d+%?|\d+%?))?\s*\)\s*$")]
        private static partial Regex RgbColorRegex();

        [GeneratedRegex(@"^(?i)\s*hsla?\(\s*(\d+\.\d+|\d+)(?:deg|rad|turn)?\s*[\s,]+(\d+\.\d+|\d+)%\s*[\s,]+(\d+\.\d+|\d+)%(?:\s*[\s,/]\s*(\d+\.\d+%?|\d+%?))?\s*\)\s*$")]
        private static partial Regex HslColorRegex();
    }
}
