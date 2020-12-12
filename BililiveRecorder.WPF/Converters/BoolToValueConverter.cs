using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BililiveRecorder.WPF.Converters
{
    internal class BoolToValueConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(nameof(TrueValue), typeof(object), typeof(BoolToValueConverter), new PropertyMetadata(null));
        public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(nameof(FalseValue), typeof(object), typeof(BoolToValueConverter), new PropertyMetadata(null));

        public object TrueValue { get => GetValue(TrueValueProperty); set => SetValue(TrueValueProperty, value); }
        public object FalseValue { get => GetValue(FalseValueProperty); set => SetValue(FalseValueProperty, value); }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return FalseValue;
            }
            else
            {
                return (bool)value ? TrueValue : FalseValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.Equals(TrueValue) : false;
        }
    }
}
