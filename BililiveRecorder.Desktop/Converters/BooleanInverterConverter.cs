using System;
using System.Globalization;
using Avalonia.Data.Converters;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    public class BooleanInverterConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b ? !b : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b ? !b : value;
    }
}
