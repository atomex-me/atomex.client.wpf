using System;
using System.Globalization;
using System.Security;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Atomex.Common;

namespace Atomex.Client.Wpf.Converters
{
    public class PasswordBoxProvider : IPasswordProvider
    {
        private readonly PasswordBox _source;

        public SecureString Password => _source?.SecurePassword;

        public PasswordBoxProvider(PasswordBox source)
        {
            _source = source;
        }
    }

    [ValueConversion(typeof(PasswordBox), typeof(IPasswordProvider))]
    [MarkupExtensionReturnType(typeof(PasswordBoxConverter))]
    public class PasswordBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new PasswordBoxProvider((PasswordBox)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("No conversion.");
        }
    }
}