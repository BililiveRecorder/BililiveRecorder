using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BililiveRecorder.WPF.Converters
{
    internal class PercentageToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const double a = 1d;
            const double b = 6d;
            const double c = 100d;
            const double d = 2.2d;

            var x = (double)value * 100d;
            var y = x < (c - a) ? c - Math.Pow(Math.Abs(x - c + a), d) / b : x > (c + a) ? c - Math.Pow(x - c - a, d) / b : c;
            return new SolidColorBrush(GradientPick(Math.Max(y, 0d) / 100d, Colors.Red, Colors.Yellow, Colors.Lime));
            Color GradientPick(double percentage, Color c1, Color c2, Color c3) => percentage < 0.5 ? ColorInterp(c1, c2, percentage / 0.5) : percentage == 0.5 ? c2 : ColorInterp(c2, c3, (percentage - 0.5) / 0.5);
            Color ColorInterp(Color start, Color end, double percentage) => Color.FromRgb(LinearInterp(start.R, end.R, percentage), LinearInterp(start.G, end.G, percentage), LinearInterp(start.B, end.B, percentage));
            byte LinearInterp(byte start, byte end, double percentage) => (byte)(start + Math.Round(percentage * (end - start)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
