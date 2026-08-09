using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    public class BoolToValueConverter : AvaloniaObject, IValueConverter
    {
        public static readonly StyledProperty<object?> TrueValueProperty =
            AvaloniaProperty.Register<BoolToValueConverter, object?>(nameof(TrueValue));
        public static readonly StyledProperty<object?> FalseValueProperty =
            AvaloniaProperty.Register<BoolToValueConverter, object?>(nameof(FalseValue));

        public object? TrueValue
        {
            get => GetValue(TrueValueProperty);
            set => SetValue(TrueValueProperty, value);
        }

        public object? FalseValue
        {
            get => GetValue(FalseValueProperty);
            set => SetValue(FalseValueProperty, value);
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value == null ? FalseValue : (bool)value ? TrueValue : FalseValue;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value != null && value.Equals(TrueValue);
    }
}
