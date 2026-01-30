using Avalonia.Data.Converters;
using Avalonia.Media;
using PathPilot.Core.Models;
using System;
using System.Globalization;

namespace PathPilot.Desktop.Converters;

public class GemColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SocketColor color)
        {
            return color switch
            {
                SocketColor.Red => Color.FromRgb(255, 77, 77),      // Strength (Red)
                SocketColor.Green => Color.FromRgb(77, 255, 136),   // Dexterity (Green)
                SocketColor.Blue => Color.FromRgb(77, 136, 255),    // Intelligence (Blue)
                SocketColor.White => Color.FromRgb(255, 255, 255),  // White (Prismatic)
                _ => Color.FromRgb(136, 136, 136)                    // Default (Gray)
            };
        }
        
        return Color.FromRgb(136, 136, 136);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}