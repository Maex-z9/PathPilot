using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PathPilot.Desktop.Converters;

public class CompletedTextDecorationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCompleted && isCompleted)
        {
            return TextDecorations.Strikethrough;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
