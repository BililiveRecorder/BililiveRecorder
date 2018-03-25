using BililiveRecorder.Core;
using System;
using System.Globalization;
using System.Windows.Data;

namespace BililiveRecorder.WPF
{
    class RecordStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RecordedRoom room)
            {
                int i = (room.IsMonitoring ? 1 : 0) + (room.IsRecording ? 2 : 0);
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
