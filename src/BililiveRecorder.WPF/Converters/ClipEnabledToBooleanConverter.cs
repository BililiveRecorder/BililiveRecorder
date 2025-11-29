using System;
using System.Globalization;
using System.Windows.Data;

namespace BililiveRecorder.WPF.Converters
{
    internal class ClipEnabledToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
            // return !(value is EnabledFeature v) || (EnabledFeature.RecordOnly != v);
            // TODO fix me
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
