using System.Windows;
using System.Windows.Controls;

namespace Atomex.Client.Wpf.Helpers
{
    public class ComboBoxHelper
    {
        public static readonly DependencyProperty CornerRadiusProperty
            = DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(ComboBoxHelper),
                new FrameworkPropertyMetadata(new CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PlaceHolderProperty
            = DependencyProperty.RegisterAttached(
                "PlaceHolder",
                typeof(string),
                typeof(ComboBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static CornerRadius GetCornerRadius(ComboBox comboBox) =>
            (CornerRadius)comboBox.GetValue(CornerRadiusProperty);
        public static void SetCornerRadius(ComboBox comboBox, object value) =>
            comboBox.SetValue(CornerRadiusProperty, value);

        public static string GetPlaceHolder(ComboBox comboBox) =>
            (string)comboBox.GetValue(PlaceHolderProperty);
        public static void SetPlaceHolder(ComboBox comboBox, string value) =>
            comboBox.SetValue(PlaceHolderProperty, value);
    }
}