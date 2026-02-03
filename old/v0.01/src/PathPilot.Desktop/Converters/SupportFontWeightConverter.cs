using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PathPilot.Desktop.Converters;

public class SupportFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSupport)
        {
            return isSupport ? FontWeight.Normal : FontWeight.SemiBold;
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
