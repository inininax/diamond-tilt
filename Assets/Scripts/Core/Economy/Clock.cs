using System;
using System.Globalization;

namespace DiamondTilt.Core
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; }

        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public void Advance(TimeSpan span) => UtcNow += span;
        public void AdvanceDays(int days) => UtcNow += TimeSpan.FromDays(days);
    }

    public static class TimeKeys
    {
        private const string DayFormat = "yyyy-MM-dd";
        private const string SeasonFormat = "yyyy-MM";

        public static string DayKey(DateTime utc) => utc.ToString(DayFormat, CultureInfo.InvariantCulture);
        public static string SeasonId(DateTime utc) => utc.ToString(SeasonFormat, CultureInfo.InvariantCulture);
        public static bool TryParseDayKey(string dayKey, out DateTime value)
            => DateTime.TryParseExact(dayKey, DayFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
        public static DateTime Today(DateTime utc) => utc.Date;
    }
}
