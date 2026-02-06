using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace PathPilot.Desktop.Converters;

public class GemIconPathToBitmapConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap?> _cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        return _cache.GetOrAdd(path, p =>
        {
            try
            {
                if (!File.Exists(p))
                    return null;
                return new Bitmap(p);
            }
            catch
            {
                return null;
            }
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
