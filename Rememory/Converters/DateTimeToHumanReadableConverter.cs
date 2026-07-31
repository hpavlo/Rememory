using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using Rememory.Models;
using System;

namespace Rememory.Converters
{
    public partial class DateTimeToHumanReadableConverter : IValueConverter
    {
        private readonly SettingsContext _settingsContext = App.Current.SettingsContext;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var currentTime = DateTime.Now;
            var clipTime = (DateTime)value;

            if (_settingsContext.TimestampFormat == TimestampFormat.Absolute)
            {
                int days = (currentTime.Date - clipTime.Date).Days;
                if (days > 0)
                {
                    return clipTime.ToShortDateString();
                }
                else
                {
                    return clipTime.ToShortTimeString();
                }
            }

            var elapsed = currentTime - clipTime;

            // See https://www.unicode.org/cldr/charts/46/supplemental/language_plural_rules.html
            return elapsed switch
            {
                { Days: > 0 } => "/Clipboard/ClipFooter_Time_DaysAgo/Text".GetLocalizedFormatResource(elapsed.Days),
                { Hours: > 0 } => "/Clipboard/ClipFooter_Time_HoursAgo/Text".GetLocalizedFormatResource(elapsed.Hours),
                { Minutes: > 0 } => "/Clipboard/ClipFooter_Time_MinutesAgo/Text".GetLocalizedFormatResource(elapsed.Minutes),
                { Seconds: > 0 } => "/Clipboard/ClipFooter_Time_SecondsAgo/Text".GetLocalizedFormatResource(elapsed.Seconds),
                _ => "/Clipboard/ClipFooter_Time_Now/Text".GetLocalizedResource()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public enum TimestampFormat
    {
        Absolute,
        Relative
    }
}
