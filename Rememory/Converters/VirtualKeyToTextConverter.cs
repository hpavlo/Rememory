using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Rememory.Helper;
using System;
using Windows.System;

namespace Rememory.Converters
{
    public partial class VirtualKeyToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var key = (int)value;
            return key switch
            {
                0x12 or 0xA4 or 0xA5 => CreateTextBlock("Alt"),                                 // VK_MENU or VM_LMENU or VK_RMENU
                0x14 => CreateTextBlock("CapsLock"),                                            // VK_CAPITAL
                0x20 => CreateFontIcon("\uE75D"),                                               // VK_SPACE
                0x25 => CreateFontIcon("\uF08D"),                                               // VK_LEFT
                0x26 => CreateFontIcon("\uF090"),                                               // VK_UP
                0x27 => CreateFontIcon("\uF08F"),                                               // VK_RIGHT
                0x28 => CreateFontIcon("\uF08E"),                                               // VK_DOWN
                >= 0x30 and <= 0x39 => CreateTextBlock($"{key % 0x10}"),                        // VK_NUMBER...
                >= 0x60 and <= 0x69 => CreateTextBlock($"NumPad {key % 0x10}"),                 // VK_NUMPAD...
                0x6A => CreateFontIcon("\uE947"),                                               // VK_MULTIPLY
                0x6B => CreateFontIcon("\uE948"),                                               // VK_ADD
                0x6D => CreateFontIcon("\uE949"),                                               // VK_SUBTRACT
                0x6E => CreateTextBlock("."),                                                   // VK_DECIMAL
                0x6F => CreateFontIcon("\uE94A"),                                               // VK_DIVIDE
                // VK_LWIN or VK_RWIN
                0x5B or 0x5C => CreatePathIcon(@"M4.80825e-07 2C4.80825e-07 0.895431 0.895431 4.80825e-07 2 4.80825e-07H11V11H4.80825e-07V2Z
                                             M22 3.93403e-07C23.1046 4.41685e-07 24 0.895431 24 2V11L13 11L13 0L22 3.93403e-07Z
                                             M24 22C24 23.1046 23.1046 24 22 24L13 24L13 13L24 13L24 22Z
                                             M2 24C0.895431 24 4.41685e-07 23.1046 3.93403e-07 22L0 13L11 13L11 24H2Z", 24, 24),
                0xAD => CreateFontIcon("\uE74F"),                                               // VK_VOLUME_MUTE
                0xAE => CreateFontIcon("\uE993"),                                               // VK_VOLUME_DOWN
                0xAF => CreateFontIcon("\uE995"),                                               // VK_VOLUME_UP
                0xB0 => CreateTextBlock("Next Track"),                                          // VK_MEDIA_NEXT_TRACK
                0xB1 => CreateTextBlock("Previous Track"),                                      // VK_MEDIA_PREV_TRACK
                0xB2 => CreateTextBlock("Stop Media"),                                          // VK_MEDIA_STOP
                0xB3 => CreateTextBlock("Play/Pause Media"),                                    // VK_MEDIA_PLAY_PAUSE
                >= 0xBA and <= 0xE2 => CreateTextBlock(KeyboardHelper.GetCharFromKey(key)),     // VK_OEM...
                _ => CreateTextBlock(((VirtualKey)key).ToString())
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private TextBlock CreateTextBlock(string text)
        {
            return new TextBlock()
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Text = text
            };
        }

        private FontIcon CreateFontIcon(string glyph)
        {
            return new FontIcon()
            {
                Glyph = glyph
            };
        }

        private Viewbox CreatePathIcon(string pathData, double width, double height)
        {
            var winIcon = XamlReader.Load($@"
                <PathIcon xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          Data=""{pathData}"" />") as PathIcon;
            var winIconContainer = new Viewbox
            {
                Child = winIcon,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = width,
                Width = height
            };
            return winIconContainer;
        }
    }
}
