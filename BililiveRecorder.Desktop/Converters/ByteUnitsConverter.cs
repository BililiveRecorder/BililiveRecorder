using System;
using System.Globalization;
using Avalonia.Data.Converters;

#nullable enable
namespace BililiveRecorder.Desktop.Converters
{
    public class ByteUnitsConverter : IValueConverter
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                double size = bytes;
                int unitIndex = 0;
                while (size >= 1024 && unitIndex < Units.Length - 1)
                {
                    size /= 1024;
                    unitIndex++;
                }
                return $"{size:F2} {Units[unitIndex]}";
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
