using System.Windows;
using Atomix.Client.Wpf.Controls;

namespace Atomix.Client.Wpf.Common
{
    public static class ChildWindowHelper
    {
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty
            .RegisterAttached(
                name: "IsOpen",
                propertyType: typeof(bool),
                ownerType: typeof(ChildWindowHelper),
                defaultMetadata: new PropertyMetadata(IsOpenChanged));

        public static bool GetIsOpen(
            DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsOpenProperty);
        }

        public static void SetIsOpen(
            DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(dp: IsOpenProperty, value: value);
        }

        private static void IsOpenChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (!Env.IsInDesignerMode())
                return;

            d.SetValue(dp: ChildWindow.IsOpenProperty, value: e.NewValue);
        }
    }
}