using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PathPilot.Desktop.Converters;

public class RarityColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rarity = value?.ToString()?.ToUpperInvariant() ?? "";

        return rarity switch
        {
            "UNIQUE" => new SolidColorBrush(Color.Parse("#af6025")),  // Orange
            "RARE" => new SolidColorBrush(Color.Parse("#ffff77")),    // Yellow
            "MAGIC" => new SolidColorBrush(Color.Parse("#8888ff")),   // Blue
            "NORMAL" => new SolidColorBrush(Color.Parse("#c8c8c8")),  // White/Gray
            _ => new SolidColorBrush(Color.Parse("#ffffff"))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
