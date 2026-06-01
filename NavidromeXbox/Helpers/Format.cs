using System;

namespace NavidromeXbox.Helpers
{
    /// <summary>Small formatting helpers for durations, sizes and counts shown on the TV.</summary>
    public static class Format
    {
        /// <summary>Seconds → "m:ss" or "h:mm:ss".</summary>
        public static string Duration(int? seconds)
        {
            if (seconds == null || seconds < 0) return "0:00";
            var t = TimeSpan.FromSeconds(seconds.Value);
            return t.TotalHours >= 1
                ? string.Format("{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds)
                : string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
        }

        public static string Duration(TimeSpan t) =>
            t.TotalHours >= 1
                ? string.Format("{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds)
                : string.Format("{0}:{1:00}", t.Minutes, t.Seconds);

        /// <summary>A friendly "1 hr 24 min" style total for an album / playlist.</summary>
        public static string LongDuration(int? seconds)
        {
            if (seconds == null || seconds <= 0) return "";
            var t = TimeSpan.FromSeconds(seconds.Value);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours} hr {t.Minutes} min";
            if (t.TotalMinutes >= 1) return $"{t.Minutes} min";
            return $"{t.Seconds} sec";
        }

        public static string TrackCount(int n) => n == 1 ? "1 track" : $"{n} tracks";

        public static string Year(int? year) => year.HasValue && year > 0 ? year.Value.ToString() : "";
    }
}
