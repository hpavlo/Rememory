using Rememory.Contracts;
using Rememory.Models;
using System;

namespace Rememory.Services
{
    /// <summary>
    /// Cleans the data if the retention period of items has expired.
    /// Check the data once in 24 hours
    /// </summary>
    public class CleanupDataService(IClipboardService clipboardService) : ICleanupDataService
    {
        private SettingsContext _settingsContext = SettingsContext.Instance;
        private IClipboardService _clipboardService = clipboardService;
        private DateTime _lastCleanupTime = DateTime.MinValue;

        public void Cleanup()
        {
            if ((CleanupTimeSpan)_settingsContext.CleanupTimeSpanIndex != CleanupTimeSpan.None &&
                DateTime.Now.Subtract(_lastCleanupTime).Days > 0)
            {
                _lastCleanupTime = DateTime.Now;
                _clipboardService.DeleteOldClips(RoundToNearestDay(_lastCleanupTime).Subtract(GetTimeSpanFromSettings()), _settingsContext.CleanFavoriteItems);
            }
        }

        private DateTime RoundToNearestDay(DateTime dateTime)
        {
            if ((CleanupTimeSpan)_settingsContext.CleanupTimeSpanIndex != CleanupTimeSpan.Day)
            {
                const int roundIntervalDays = 1;
                var halfIntervalTicks = TimeSpan.TicksPerDay * roundIntervalDays / 2;
                return new DateTime((dateTime.Ticks + halfIntervalTicks) / TimeSpan.TicksPerDay * TimeSpan.TicksPerDay);
            }
            return dateTime;
        }

        private TimeSpan GetTimeSpanFromSettings()
        {
            return (CleanupTimeSpan)_settingsContext.CleanupTimeSpanIndex switch
            {
                CleanupTimeSpan.Day => new TimeSpan(1, 0, 0, 0),
                CleanupTimeSpan.Week => new TimeSpan(7, 0, 0, 0),
                CleanupTimeSpan.Month => new TimeSpan(30, 0, 0, 0),
                _ => new TimeSpan()
            };
        }
    }

    public enum CleanupTimeSpan
    {
        Day = 0,
        Week = 1,
        Month = 2,
        None = 3
    }
}
