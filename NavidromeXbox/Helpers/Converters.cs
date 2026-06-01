using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NavidromeXbox.Helpers
{
    /// <summary>Seconds (int) → "m:ss". Used by song-row track-length labels.</summary>
    public sealed class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int i) return Format.Duration(i);
            if (value is int?) return Format.Duration((int?)value);
            return "0:00";
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
    }

    /// <summary>bool isStarred → a filled or outline heart glyph (Segoe MDL2 Assets).</summary>
    public sealed class StarGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool starred = value is bool b && b;
            return starred ? "\uEB52" /* HeartFill */ : "\uEB51" /* Heart */;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
    }

    /// <summary>Non-empty string / non-null → Visible; otherwise Collapsed. Invert with parameter "invert".</summary>
    public sealed class EmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool has = value != null && !(value is string s && string.IsNullOrWhiteSpace(s));
            bool invert = parameter as string == "invert";
            if (invert) has = !has;
            return has ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
    }

    /// <summary>bool → Visibility (true = Visible). Invert with parameter "invert".</summary>
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool v = value is bool b && b;
            if (parameter as string == "invert") v = !v;
            return v ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is Visibility vis && vis == Visibility.Visible;
    }
}
