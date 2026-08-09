using System;
using System.Globalization;
using Avalonia.Data.Converters;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    public class IsNullToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value == null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
