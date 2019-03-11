using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Atomix.Client.Wpf.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    [MarkupExtensionReturnType(typeof(StringToUpperCaseConverter))]
    public class StringToUpperCaseConverter : MarkupExtension, IValueConverter
    {
        #region MarkupExtension "overrides"

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new StringToUpperCaseConverter();
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && targetType == typeof(string))
                return str.ToUpper(culture);

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}