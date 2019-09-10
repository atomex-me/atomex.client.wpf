using System.Windows;
using System.Windows.Controls;

namespace Atomex.Client.Wpf.Helpers
{
    public class TabItemHelper
    {
        public static readonly DependencyProperty GlyphProperty
            = DependencyProperty.RegisterAttached(
                "Glyph",
                typeof(object),
                typeof(TabItemHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static object GetGlyph(TabItem tabItem) => tabItem.GetValue(GlyphProperty);
        public static void SetGlyph(TabItem tabItem, object value) => tabItem.SetValue(GlyphProperty, value);
    }
}