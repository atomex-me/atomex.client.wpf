using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Atomex.Client.Wpf.Converters
{
    [ValueConversion(typeof(decimal), typeof(string))]
    [MarkupExtensionReturnType(typeof(AmountToStringConverter))]
    public class AmountToStringConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                throw new InvalidOperationException("Invalid values");

            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return "-";

            var amount = (decimal) values[0];
            var format = (string) values[1];

            return amount.ToString(format, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (!(value is string s))
                throw new InvalidOperationException("Invalid value");

            return new object[]
            {
                string.IsNullOrEmpty(s) ? 0.0m : decimal.Parse(s, culture),
                null
            };
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new AmountToStringConverter();
        }
    }
}