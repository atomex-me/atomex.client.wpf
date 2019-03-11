using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Atomix.Client.Wpf.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    [MarkupExtensionReturnType(typeof(BoolToVisibilityConverter))]
    public class BoolToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public BoolToVisibilityConverter()
        {
            FalseEquivalent = Visibility.Hidden;
            OppositeBoolValue = false;
        }

        public Visibility FalseEquivalent { get; set; }
        public bool OppositeBoolValue { get; set; }

        #region MarkupExtension "overrides"

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new BoolToVisibilityConverter
            {
                FalseEquivalent = FalseEquivalent,
                OppositeBoolValue = OppositeBoolValue
            };
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && targetType == typeof(Visibility))
            {
                if (OppositeBoolValue)
                    return boolValue
                        ? FalseEquivalent
                        : Visibility.Visible;

                return boolValue
                    ? Visibility.Visible
                    : FalseEquivalent;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                if (OppositeBoolValue)
                    return visibility != Visibility.Visible;

                return visibility == Visibility.Visible;
            }
            return value;
        }

        #endregion
    }
}