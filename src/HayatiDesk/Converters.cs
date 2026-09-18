// Closes: U1, R4
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HayatiDesk;

public class ColorToBrushConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Brush> _cache = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            return _cache.GetOrAdd(hexColor, key =>
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(key);
                    var brush = new SolidColorBrush(color);
                    brush.Freeze(); // U1: Freeze brush
                    return brush;
                }
                catch
                {
                    return Brushes.LightGray;
                }
            });
        }
        return Brushes.LightGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // U1: ConvertBack returns Binding.DoNothing
        return Binding.DoNothing;
    }
}

public class StrikethroughConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool completed && completed)
        {
            return TextDecorations.Strikethrough;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

// R4: NullOrEmptyToVisibilityConverter
public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
