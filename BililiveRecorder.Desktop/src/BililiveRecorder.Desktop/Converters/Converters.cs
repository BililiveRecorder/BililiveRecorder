using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using BililiveRecorder.Desktop.Controls;
using BililiveRecorder.Desktop.Localization;
using Serilog.Events;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    /// <summary>
    /// 枚举值与 RadioButton IsChecked 的双向转换（ConverterParameter 传目标枚举值）。
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public static readonly EnumToBooleanConverter Instance = new EnumToBooleanConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null || parameter is null)
                return false;
            return value.Equals(parameter);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true && parameter is not null && parameter.GetType() == targetType)
                return parameter;
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// 枚举值与目标值不相等时为 true（用于 IsVisible，如“未关闭分段”时显示数值输入）。
    /// </summary>
    public class EnumNotToBooleanConverter : IValueConverter
    {
        public static readonly EnumNotToBooleanConverter Instance = new EnumNotToBooleanConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null || parameter is null)
                return true;
            return !value.Equals(parameter);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    public class BoolToRecordingBrushConverter : IValueConverter
    {
        public static readonly BoolToRecordingBrushConverter Instance = new BoolToRecordingBrushConverter();

        private static readonly IBrush recordingBrush = new SolidColorBrush(Colors.Red);
        private static readonly IBrush monitoringBrush = new SolidColorBrush(Colors.DarkOrange);

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? recordingBrush : monitoringBrush;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>
    /// bool → 画刷。用于弹幕连接 / 直播状态图标的颜色（与 WPF ConverterResources.xaml 一致）。
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        /// <summary>弹幕连接状态：已连接绿色、断开红色。</summary>
        public static readonly BoolToBrushConverter DanmakuConnected = new BoolToBrushConverter
        {
            TrueBrush = new SolidColorBrush(Color.Parse("#8BC34A")),
            FalseBrush = new SolidColorBrush(Colors.Red),
        };

        /// <summary>直播状态：直播中红色、未开播灰色。</summary>
        public static readonly BoolToBrushConverter Streaming = new BoolToBrushConverter
        {
            TrueBrush = new SolidColorBrush(Colors.Red),
            FalseBrush = new SolidColorBrush(Color.Parse("#999999")),
        };

        public IBrush? TrueBrush { get; init; }

        public IBrush? FalseBrush { get; init; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? this.TrueBrush : this.FalseBrush;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>
    /// bool → 本地化文本。ConverterParameter 传 "trueKey|falseKey"。
    /// </summary>
    public class BoolToLocalizedTextConverter : IValueConverter
    {
        public static readonly BoolToLocalizedTextConverter Instance = new BoolToLocalizedTextConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool flag || parameter is not string keys)
                return null;

            var parts = keys.Split('|');
            var key = flag ? parts[0] : parts.Length > 1 ? parts[1] : parts[0];
            return LocalizationService.Instance[key];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>
    /// bool → double。ConverterParameter 传 "trueValue|falseValue"。
    /// </summary>
    public class BoolToDoubleConverter : IValueConverter
    {
        public static readonly BoolToDoubleConverter Instance = new BoolToDoubleConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool flag || parameter is not string values)
                return AvaloniaProperty.UnsetValue;

            var parts = values.Split('|');
            var text = flag ? parts[0] : parts.Length > 1 ? parts[1] : parts[0];
            return double.Parse(text, CultureInfo.InvariantCulture);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>
    /// 录制速度比（Stats.DurationRatio）→ 速度药丸背景色，红→黄→绿渐变。
    /// 与 WPF RatioToColorBrushConverter 逻辑一致；NaN（无数据）为 AliceBlue。
    /// </summary>
    public class RatioToBrushConverter : IValueConverter
    {
        public static readonly RatioToBrushConverter Instance = new RatioToBrushConverter();

        private static readonly IBrush disabledBrush = new SolidColorBrush(Colors.AliceBlue);
        private static readonly IBrush[] colorMap = BuildColorMap();

        private static IBrush[] BuildColorMap()
        {
            return Enumerable
                .Range(0, 21)
                .Select(i => new SolidColorBrush(GradientPick(i / 20d, Colors.Red, Colors.Yellow, Colors.Lime)))
                .ToArray();

            static Color GradientPick(double percentage, Color c1, Color c2, Color c3) =>
                percentage < 0.5 ? ColorInterp(c1, c2, percentage / 0.5) : percentage == 0.5 ? c2 : ColorInterp(c2, c3, (percentage - 0.5) / 0.5);

            static Color ColorInterp(Color start, Color end, double percentage) =>
                Color.FromRgb(LinearInterp(start.R, end.R, percentage), LinearInterp(start.G, end.G, percentage), LinearInterp(start.B, end.B, percentage));

            static byte LinearInterp(byte start, byte end, double percentage) =>
                (byte)(start + Math.Round(percentage * (end - start)));
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var input = value is double d ? d : double.NaN;

            if (double.IsNaN(input))
                return disabledBrush;

            var i = (int)Math.Ceiling((1.1d - Math.Abs((1d - input) * 4d)) * 20d);
            return i switch
            {
                < 0 => colorMap[0],
                > 20 => colorMap[20],
                _ => colorMap[i],
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>
    /// 录制速度比 → 上/下箭头图标（低于 0.97 下箭头、高于 1.03 上箭头、其余不显示）。
    /// 与 WPF RatioToArrowIconConverter 逻辑一致。
    /// </summary>
    public class RatioToArrowGeometryConverter : IValueConverter
    {
        public static readonly RatioToArrowGeometryConverter Instance = new RatioToArrowGeometryConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is double num
                ? (num < 0.97 ? RoomCardIcons.ArrowDownBold : num > 1.03 ? RoomCardIcons.ArrowUpBold : null)
                : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    public class LogEventLevelToBrushConverter : IValueConverter
    {
        public static readonly LogEventLevelToBrushConverter Instance = new LogEventLevelToBrushConverter();

        private static readonly IBrush debugBrush = new SolidColorBrush(Colors.Gray);
        private static readonly IBrush infoBrush = new SolidColorBrush(Colors.Green);
        private static readonly IBrush warningBrush = new SolidColorBrush(Colors.DarkOrange);
        private static readonly IBrush errorBrush = new SolidColorBrush(Colors.Red);
        private static readonly IBrush fatalBrush = new SolidColorBrush(Colors.DarkRed);

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                LogEventLevel.Verbose or LogEventLevel.Debug => debugBrush,
                LogEventLevel.Information => infoBrush,
                LogEventLevel.Warning => warningBrush,
                LogEventLevel.Error => errorBrush,
                LogEventLevel.Fatal => fatalBrush,
                _ => debugBrush,
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }
}
