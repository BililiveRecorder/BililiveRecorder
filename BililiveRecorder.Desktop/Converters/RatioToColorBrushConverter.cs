using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    public class RatioToColorBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double ratio)
            {
                if (double.IsNaN(ratio))
                    return new SolidColorBrush(Colors.Gray);
                if (ratio < 0.8)
                    return new SolidColorBrush(Colors.Red);
                if (ratio < 0.95)
                    return new SolidColorBrush(Colors.Yellow);
                return new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
