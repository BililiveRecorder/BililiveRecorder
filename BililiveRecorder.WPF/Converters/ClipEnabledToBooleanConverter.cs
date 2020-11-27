using System;
using System.Globalization;
using System.Windows.Data;
using BililiveRecorder.FlvProcessor;

namespace BililiveRecorder.WPF.Converters
{
    internal class ClipEnabledToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is EnabledFeature v) || (EnabledFeature.RecordOnly != v);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
