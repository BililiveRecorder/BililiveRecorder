using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BililiveRecorder.WPF.Converters
{
    internal class ShortRoomIdToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i ? i == 0 ? Visibility.Collapsed : Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
