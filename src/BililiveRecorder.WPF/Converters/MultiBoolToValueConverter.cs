using System;
using System.Globalization;
using System.Windows.Data;

#nullable enable
namespace BililiveRecorder.WPF.Converters
{
    public class MultiBoolToValueConverter : IMultiValueConverter
    {
        public object? FalseValue { get; set; }
        public object? TrueValue { get; set; }

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if ((value is bool boolean) && boolean == false)
                {
                    return this.FalseValue;
                }
            }
            return this.TrueValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
