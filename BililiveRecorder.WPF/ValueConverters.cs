using System;
using System.Globalization;
using System.Windows.Data;

namespace BililiveRecorder.WPF
{
    class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    class RecordStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value[0] is bool IsMonitoring && value[1] is bool IsRecording)
            {
                int i = (IsMonitoring ? 1 : 0) + (IsRecording ? 2 : 0);
                switch (i)
                {
                    case 0:
                        return "闲置中";
                    case 1:
                        return "监控中";
                    case 2:
                    case 3:
                        return "录制中";
                    default:
                        return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
