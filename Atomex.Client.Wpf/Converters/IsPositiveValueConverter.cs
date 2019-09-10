using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Atomex.Client.Wpf.Converters
{
    [MarkupExtensionReturnType(typeof(IsPositiveValueConverter))]
    public class IsPositiveValueConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new IsPositiveValueConverter();           
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is decimal decimalValue)
                return decimalValue >= 0;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}