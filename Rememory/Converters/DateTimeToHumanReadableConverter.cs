using Microsoft.UI.Xaml.Data;
using Rememory.Helper;
using System;

namespace Rememory.Converters
{
    public class DateTimeToHumanReadableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var elapsed = DateTime.Now - ((DateTime)value);
            return elapsed switch
            {
                { Days: > 0 } => "DaysAgo".GetLocalizedFormatResource(elapsed.Days),
                { Hours: > 0 } => "HoursAgo".GetLocalizedFormatResource(elapsed.Hours),
                { Minutes: > 0 } => "MinutesAgo".GetLocalizedFormatResource(elapsed.Minutes),
                { Seconds: > 0 } => "SecondsAgo".GetLocalizedFormatResource(elapsed.Seconds),
                _ => "Now".GetLocalizedResource()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
