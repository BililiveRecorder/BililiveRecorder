using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    public class MultiBoolToValueConverter : AvaloniaObject, IMultiValueConverter
    {
        public static readonly StyledProperty<object?> TrueValueProperty =
            AvaloniaProperty.Register<MultiBoolToValueConverter, object?>(nameof(TrueValue));
        public static readonly StyledProperty<object?> FalseValueProperty =
            AvaloniaProperty.Register<MultiBoolToValueConverter, object?>(nameof(FalseValue));

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

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values.All(v => v is bool b && b) ? TrueValue : FalseValue;
    }
}
