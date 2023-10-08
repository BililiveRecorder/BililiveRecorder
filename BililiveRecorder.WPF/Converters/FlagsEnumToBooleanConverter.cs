using System;
using System.Globalization;
using System.Windows.Data;

namespace BililiveRecorder.WPF.Converters
{
    internal class FlagsEnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Enum)value).HasFlag((Enum)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
