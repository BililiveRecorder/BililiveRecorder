using BililiveRecorder.FlvProcessor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace BililiveRecorder.WPF
{
    internal class ClipButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return Visibility.Visible;
            return EnabledFeature.RecordOnly == ((EnabledFeature)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    internal class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class EnumToBooleanConverter : IValueConverter
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

    internal class BooleanInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    internal class RecordStatusConverter : IMultiValueConverter
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

    internal class BoolToValueConverter<T> : IValueConverter
    {
        public T FalseValue { get; set; }
        public T TrueValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return FalseValue;
            }
            else
            {
                return (bool)value ? TrueValue : FalseValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.Equals(TrueValue) : false;
        }
    }

    internal class BoolToStringConverter : BoolToValueConverter<string> { }

}
