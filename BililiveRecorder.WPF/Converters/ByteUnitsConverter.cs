using System;
using System.Globalization;
using System.Windows.Data;

namespace BililiveRecorder.WPF.Converters
{
    public class ByteUnitsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const long Bytes = 1;
            const long KiB = Bytes * 1024;
            const long MiB = KiB * 1024;
            const long GiB = MiB * 1024;
            const long TiB = GiB * 1024;
            const double d_KiB = KiB;
            const double d_MiB = MiB;
            const double d_GiB = GiB;
            const double d_TiB = TiB;

            var input = (long)value;

            return input switch
            {
                < KiB => $"{input} {nameof(Bytes)}",
                < MiB => $"{input / d_KiB:F2} {nameof(KiB)}",
                < GiB => $"{input / d_MiB:F2} {nameof(MiB)}",
                < TiB => $"{input / d_GiB:F2} {nameof(GiB)}",
                _ => $"{input / d_TiB:F2} {nameof(TiB)}"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
