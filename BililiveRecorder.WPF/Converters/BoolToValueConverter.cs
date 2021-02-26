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

        public object TrueValue { get => this.GetValue(TrueValueProperty); set => this.SetValue(TrueValueProperty, value); }
        public object FalseValue { get => this.GetValue(FalseValueProperty); set => this.SetValue(FalseValueProperty, value); }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value == null ? this.FalseValue : (bool)value ? this.TrueValue : this.FalseValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null && value.Equals(this.TrueValue);
    }
}
