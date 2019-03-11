using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Atomix.Client.Wpf.Behaviors
{
    public class PasswordBoxHasPasswordBehavior : Behavior<PasswordBox>
    {
        public static readonly DependencyProperty HasPasswordProperty
            = DependencyProperty.RegisterAttached(
                "HasPassword",
                typeof(bool),
                typeof(PasswordBoxHasPasswordBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)); //, OnHasPasswordPropertyChanged));


        public static bool GetHasPassword(PasswordBox passwordBox) => (bool)passwordBox.GetValue(HasPasswordProperty);
        public static void SetHasPassword(PasswordBox passwordBox, object value) => passwordBox.SetValue(HasPasswordProperty, value);

        private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = (PasswordBox)sender;
            SetHasPassword(passwordBox, passwordBox.Password.Length > 0);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += PasswordBoxPasswordChanged;
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
                AssociatedObject.PasswordChanged -= PasswordBoxPasswordChanged;

            base.OnDetaching();
        }
    }
}