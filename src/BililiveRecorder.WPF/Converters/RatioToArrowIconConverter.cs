using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#nullable enable
namespace BililiveRecorder.WPF.Converters
{
    public class RatioToArrowIconConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty UpArrowProperty = DependencyProperty.Register(nameof(UpArrow), typeof(object), typeof(BoolToValueConverter), new PropertyMetadata(null));
        public static readonly DependencyProperty DownArrowProperty = DependencyProperty.Register(nameof(DownArrow), typeof(object), typeof(BoolToValueConverter), new PropertyMetadata(null));

        public object UpArrow { get => this.GetValue(UpArrowProperty); set => this.SetValue(UpArrowProperty, value); }
        public object DownArrow { get => this.GetValue(DownArrowProperty); set => this.SetValue(DownArrowProperty, value); }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is double num ? num < 0.97 ? this.DownArrow : num > 1.03 ? this.UpArrow : null : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
