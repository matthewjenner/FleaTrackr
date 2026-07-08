using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FleaTrackr.App.Converters;

/// <summary>
/// Maps a watchlist row's "is alerting" flag to a background brush: a soft amber wash when an alert
/// has just fired, transparent otherwise. Used to flash a row when one of its alerts trips.
/// </summary>
public sealed class AlertHighlightConverter : IValueConverter
{
    private static readonly IBrush Highlight = new SolidColorBrush(Color.FromArgb(0x55, 0xE0, 0xA5, 0x2A));

    public static readonly AlertHighlightConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Highlight : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
