using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Atomex.Client.Wpf.Helpers
{
    public class TextBoxHelper
    {
        public static readonly DependencyProperty CornerRadiusProperty 
            = DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(new CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PlaceHolderProperty
            = DependencyProperty.RegisterAttached(
                "PlaceHolder",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FocusBorderBrushProperty 
            = DependencyProperty.RegisterAttached(
                "FocusBorderBrush",
                typeof(Brush),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty MouseOverBorderBrushProperty 
            = DependencyProperty.RegisterAttached(
                "MouseOverBorderBrush",
                typeof(Brush),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IconProperty
            = DependencyProperty.RegisterAttached(
                "Icon",
                typeof(object),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static CornerRadius GetCornerRadius(TextBox textBox) =>
            (CornerRadius)textBox.GetValue(CornerRadiusProperty);
        public static void SetCornerRadius(TextBox textBox, object value) =>
            textBox.SetValue(CornerRadiusProperty, value);

        public static string GetPlaceHolder(TextBox textBox) =>
            (string)textBox.GetValue(PlaceHolderProperty);
        public static void SetPlaceHolder(TextBox textBox, string value) =>
            textBox.SetValue(PlaceHolderProperty, value);

        public static Brush GetFocusBorderBrush(TextBox textBox) =>
            (Brush)textBox.GetValue(FocusBorderBrushProperty);
        public static void SetFocusBorderBrush(TextBox textBox, Brush value) =>
            textBox.SetValue(FocusBorderBrushProperty, value);

        public static Brush GetMouseOverBorderBrush(TextBox textBox) =>
            (Brush)textBox.GetValue(MouseOverBorderBrushProperty);
        public static void SetMouseOverBorderBrush(TextBox textBox, Brush value) =>
            textBox.SetValue(MouseOverBorderBrushProperty, value);

        public static object GetIcon(TextBox textBox) =>
            textBox.GetValue(IconProperty);
        public static void SetIcon(TextBox textBox, object value) =>
            textBox.SetValue(IconProperty, value);
    }
}