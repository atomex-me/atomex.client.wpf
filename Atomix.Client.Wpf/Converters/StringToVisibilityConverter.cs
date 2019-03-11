using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Atomix.Client.Wpf.Converters
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    [MarkupExtensionReturnType(typeof(StringToVisibilityConverter))]
    public class StringToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public StringToVisibilityConverter()
        {
            FalseEquivalent = Visibility.Collapsed;
            OppositeStringValue = false;
        }

        public Visibility FalseEquivalent { get; set; }

        public bool OppositeStringValue { get; set; }

        #region MarkupExtension "overrides"

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new StringToVisibilityConverter
            {
                FalseEquivalent = FalseEquivalent,
                OppositeStringValue = OppositeStringValue
            };
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value == null || value is string) && targetType == typeof(Visibility))
            {
                if (OppositeStringValue)
                    return string.IsNullOrEmpty((string)value)
                        ? Visibility.Visible
                        : FalseEquivalent;

                return string.IsNullOrEmpty((string)value)
                    ? FalseEquivalent
                    : Visibility.Visible;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                if (OppositeStringValue)
                    return visibility == Visibility.Visible
                        ? string.Empty 
                        : "visible";
   
                return visibility == Visibility.Visible
                    ? "visible"
                    : string.Empty;
            }
            return value;
        }

        #endregion
    }
}