using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FleaTrackr.App.Converters;

/// <summary>
/// Maps a numeric price change to a colour: positive -> green, negative -> red, zero/null ->
/// neutral. Used to tint the 48h change text. Colours are chosen to read on both light and dark
/// Fluent themes.
/// </summary>
public sealed class SignToBrushConverter : IValueConverter
{
    private static readonly IBrush Up = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43));
    private static readonly IBrush Down = new SolidColorBrush(Color.FromRgb(0xD2, 0x3F, 0x31));
    private static readonly IBrush Neutral = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

    public static readonly SignToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double n = value switch
        {
            double d => d,
            int i => i,
            _ => 0,
        };
        return n > 0 ? Up : n < 0 ? Down : Neutral;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
