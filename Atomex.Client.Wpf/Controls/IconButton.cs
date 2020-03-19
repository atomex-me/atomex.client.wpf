using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Atomex.Client.Wpf.Controls
{
    public class IconButton : Button
    {
        public static readonly DependencyProperty MouseOverBrushProperty
            = DependencyProperty.Register(
                nameof(MouseOverBrush),
                typeof(Brush),
                typeof(IconButton),
                new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty PressedBrushProperty
            = DependencyProperty.Register(
                nameof(PressedBrush),
                typeof(Brush),
                typeof(IconButton),
                new PropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty PathProperty
            = DependencyProperty.Register(
                nameof(Path),
                typeof(object),
                typeof(IconButton),
                new PropertyMetadata(null));

        public Brush MouseOverBrush
        {
            get => (Brush)GetValue(MouseOverBrushProperty);
            set => SetValue(MouseOverBrushProperty, value);
        }

        public Brush PressedBrush
        {
            get => (Brush)GetValue(PressedBrushProperty);
            set => SetValue(PressedBrushProperty, value);
        }

        public object Path
        {
            get => GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
    }
}