using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PathPilot.Desktop.Converters;

public class OverlaySupportColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSupport)
        {
            return isSupport
                ? new SolidColorBrush(Color.FromRgb(170, 170, 170))  // Gray for supports
                : new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White for active skills
        }
        return new SolidColorBrush(Color.FromRgb(255, 255, 255));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
