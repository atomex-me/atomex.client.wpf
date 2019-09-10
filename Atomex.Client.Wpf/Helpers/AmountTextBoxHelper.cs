using System.Windows;
using System.Windows.Controls;

namespace Atomex.Client.Wpf.Helpers
{
    public class AmountTextBoxHelper
    {
        public static readonly DependencyProperty CurrencyCodeProperty
            = DependencyProperty.RegisterAttached(
                "CurrencyCode",
                typeof(string),
                typeof(AmountTextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AmountInBaseProperty
            = DependencyProperty.RegisterAttached(
                "AmountInBase",
                typeof(string),
                typeof(AmountTextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BaseCurrencyCodeProperty
            = DependencyProperty.RegisterAttached(
                "BaseCurrencyCode",
                typeof(string),
                typeof(AmountTextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CurrencyCodeFontSizeProperty
            = DependencyProperty.RegisterAttached(
                "CurrencyCodeFontSize",
                typeof(double),
                typeof(AmountTextBoxHelper),
                new FrameworkPropertyMetadata(0.0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AmountInBaseFontSizeProperty
            = DependencyProperty.RegisterAttached(
                "AmountInBaseFontSize",
                typeof(double),
                typeof(AmountTextBoxHelper),
                new FrameworkPropertyMetadata(0.0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BaseCurrencyCodeFontSizeProperty
            = DependencyProperty.RegisterAttached(
                "BaseCurrencyCodeFontSize",
                typeof(double),
                typeof(AmountTextBoxHelper),
                new FrameworkPropertyMetadata(0.0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public static string GetCurrencyCode(TextBox textBox) => (string)textBox.GetValue(CurrencyCodeProperty);
        public static void SetCurrencyCode(TextBox textBox, string value) => textBox.SetValue(CurrencyCodeProperty, value);

        public static string GetAmountInBase(TextBox textBox) => (string)textBox.GetValue(AmountInBaseProperty);
        public static void SetAmountInBase(TextBox textBox, string value) => textBox.SetValue(AmountInBaseProperty, value);

        public static string GetBaseCurrencyCode(TextBox textBox) => (string)textBox.GetValue(BaseCurrencyCodeProperty);
        public static void SetBaseCurrencyCode(TextBox textBox, string value) => textBox.SetValue(BaseCurrencyCodeProperty, value);

        public static double GetCurrencyCodeFontSize(TextBox textBox) => (double)textBox.GetValue(CurrencyCodeFontSizeProperty);
        public static void SetCurrencyCodeFontSize(TextBox textBox, double value) => textBox.SetValue(CurrencyCodeFontSizeProperty, value);

        public static double GetAmountInBaseFontSize(TextBox textBox) => (double)textBox.GetValue(AmountInBaseFontSizeProperty);
        public static void SetAmountInBaseFontSize(TextBox textBox, double value) => textBox.SetValue(AmountInBaseFontSizeProperty, value);

        public static double GetBaseCurrencyCodeFontSize(TextBox textBox) => (double)textBox.GetValue(BaseCurrencyCodeFontSizeProperty);
        public static void SetBaseCurrencyCodeFontSize(TextBox textBox, double value) => textBox.SetValue(BaseCurrencyCodeFontSizeProperty, value);
    }
}