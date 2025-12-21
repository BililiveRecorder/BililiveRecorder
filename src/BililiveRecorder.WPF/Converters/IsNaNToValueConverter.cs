using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BililiveRecorder.WPF.Converters
{
    public class IsNaNToValueConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(nameof(TrueValue), typeof(object), typeof(IsNaNToValueConverter), new PropertyMetadata(null));
        public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(nameof(FalseValue), typeof(object), typeof(IsNaNToValueConverter), new PropertyMetadata(null));

        public object TrueValue { get => this.GetValue(TrueValueProperty); set => this.SetValue(TrueValueProperty, value); }
        public object FalseValue { get => this.GetValue(FalseValueProperty); set => this.SetValue(FalseValueProperty, value); }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is double d && double.IsNaN(d)) ? this.TrueValue : this.FalseValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
