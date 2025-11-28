using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public static class Converters
    {
        public static readonly IValueConverter ShortRoomIdToVisibility = new ShortRoomIdToVisibilityConverter();
        public static readonly IValueConverter BoolToConnectedColor = new BoolToColorConverter(Brushes.LightGreen, Brushes.Red);
        public static readonly IValueConverter BoolToLiveColor = new BoolToColorConverter(Brushes.Red, Brushes.Gray);
        public static readonly IValueConverter BoolToDanmakuTooltip = new BoolToStringConverter("弹幕已连接 Connected", "弹幕未连接 Disconnected");
        public static readonly IValueConverter BoolToLiveTooltip = new BoolToStringConverter("直播中 Live", "未开播 Offline");
        public static readonly IValueConverter InvertBool = new InvertBoolConverter();
        public static readonly IValueConverter RatioToColorBrush = new RatioToColorBrushConverter();
    }

    public class ShortRoomIdToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int shortId)
            {
                return shortId > 0;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        private readonly IBrush _trueBrush;
        private readonly IBrush _falseBrush;

        public BoolToColorConverter(IBrush trueBrush, IBrush falseBrush)
        {
            _trueBrush = trueBrush;
            _falseBrush = falseBrush;
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? _trueBrush : _falseBrush;
            }
            return _falseBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        private readonly string _trueString;
        private readonly string _falseString;

        public BoolToStringConverter(string trueString, string falseString)
        {
            _trueString = trueString;
            _falseString = falseString;
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? _trueString : _falseString;
            }
            return _falseString;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return false;
        }
    }

    public class RatioToColorBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double ratio)
            {
                if (double.IsNaN(ratio))
                {
                    return Brushes.Gray;
                }
                else if (ratio >= 0.98)
                {
                    return Brushes.LightGreen;
                }
                else if (ratio >= 0.95)
                {
                    return Brushes.Yellow;
                }
                else
                {
                    return Brushes.Orange;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
