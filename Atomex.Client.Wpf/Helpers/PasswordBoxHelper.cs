using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Atomex.Common;

namespace Atomex.Client.Wpf.Helpers
{
    public class PasswordBoxHelper
    {
        public static readonly DependencyProperty CornerRadiusProperty
             = DependencyProperty.RegisterAttached(
                 "CornerRadius",
                 typeof(CornerRadius),
                 typeof(PasswordBoxHelper),
                 new FrameworkPropertyMetadata(new CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PlaceHolderProperty
            = DependencyProperty.RegisterAttached(
                "PlaceHolder",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FocusBorderBrushProperty
            = DependencyProperty.RegisterAttached(
                "FocusBorderBrush",
                typeof(Brush),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty MouseOverBorderBrushProperty
            = DependencyProperty.RegisterAttached(
                "MouseOverBorderBrush",
                typeof(Brush),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IconProperty
            = DependencyProperty.RegisterAttached(
                "Icon",
                typeof(object),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CapsLockIconProperty
            = DependencyProperty.RegisterAttached(
                "CapsLockIcon",
                typeof(object),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata("!", FrameworkPropertyMetadataOptions.AffectsRender, ShowCapslockWarningChanged));

        public static readonly DependencyProperty CapsLockWarningToolTipProperty
            = DependencyProperty.RegisterAttached(
                "CapsLockWarningToolTip",
                typeof(object),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata("Caps lock is on", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RevealButtonContentProperty
            = DependencyProperty.RegisterAttached(
                "RevealButtonContent",
                typeof(object),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RevealButtonStyleProperty
            = DependencyProperty.RegisterAttached(
                "RevealButtonStyle",
                typeof(Style),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SecurePasswordBindingProperty
            = DependencyProperty.RegisterAttached(
                "SecurePassword",
                typeof(SecureString),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(new SecureString(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    AttachedPropertyValueChanged));

        private static readonly DependencyProperty PasswordBindingMarshallerProperty 
            = DependencyProperty.RegisterAttached(
                "PasswordBindingMarshaller",
                typeof(PasswordBindingMarshaller),
                typeof(PasswordBoxHelper), new PropertyMetadata());

        public static CornerRadius GetCornerRadius(PasswordBox passwordBox) => (CornerRadius)passwordBox.GetValue(CornerRadiusProperty);
        public static void SetCornerRadius(PasswordBox passwordBox, object value) => passwordBox.SetValue(CornerRadiusProperty, value);

        public static string GetPlaceHolder(PasswordBox passwordBox) => (string)passwordBox.GetValue(PlaceHolderProperty);
        public static void SetPlaceHolder(PasswordBox passwordBox, string value) => passwordBox.SetValue(PlaceHolderProperty, value);

        public static Brush GetFocusBorderBrush(PasswordBox passwordBox) => (Brush)passwordBox.GetValue(FocusBorderBrushProperty);
        public static void SetFocusBorderBrush(PasswordBox passwordBox, Brush value) => passwordBox.SetValue(FocusBorderBrushProperty, value);

        public static Brush GetMouseOverBorderBrush(PasswordBox passwordBox) => (Brush)passwordBox.GetValue(MouseOverBorderBrushProperty);
        public static void SetMouseOverBorderBrush(PasswordBox passwordBox, Brush value) => passwordBox.SetValue(MouseOverBorderBrushProperty, value);

        public static object GetIcon(PasswordBox passwordBox) => passwordBox.GetValue(IconProperty);
        public static void SetIcon(PasswordBox passwordBox, object value) => passwordBox.SetValue(IconProperty, value);

        public static object GetCapsLockIcon(PasswordBox passwordBox) => passwordBox.GetValue(CapsLockIconProperty);
        public static void SetCapsLockIcon(PasswordBox passwordBox, object value) => passwordBox.SetValue(CapsLockIconProperty, value);

        public static object GetCapsLockWarningToolTip(PasswordBox passwordBox) => passwordBox.GetValue(CapsLockWarningToolTipProperty);
        public static void SetCapsLockWarningToolTip(PasswordBox passwordBox, object value) => passwordBox.SetValue(CapsLockWarningToolTipProperty, value);

        public static object GetRevealButtonContent(PasswordBox passwordBox) => passwordBox.GetValue(RevealButtonContentProperty);
        public static void SetRevealButtonContent(PasswordBox passwordBox, object value) => passwordBox.SetValue(RevealButtonContentProperty, value);

        public static Style GetRevealButtonStyle(PasswordBox passwordBox) => (Style)passwordBox.GetValue(RevealButtonStyleProperty);
        public static void SetRevealButtonStyle(PasswordBox passwordBox, object value) => passwordBox.SetValue(RevealButtonStyleProperty, value);

        private static void ShowCapslockWarningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                var pb = (PasswordBox)d;

                pb.KeyDown -= RefreshCapslockStatus;
                pb.GotFocus -= RefreshCapslockStatus;
                pb.PreviewGotKeyboardFocus -= RefreshCapslockStatus;
                pb.LostFocus -= HandlePasswordBoxLostFocus;

                if (e.NewValue != null)
                {
                    pb.KeyDown += RefreshCapslockStatus;
                    pb.GotFocus += RefreshCapslockStatus;
                    pb.PreviewGotKeyboardFocus += RefreshCapslockStatus;
                    pb.LostFocus += HandlePasswordBoxLostFocus;
                }
            }
        }

        private static void RefreshCapslockStatus(object sender, RoutedEventArgs e)
        {
            var fe = FindCapsLockIndicator((Control)sender);
            if (fe != null)
                fe.Visibility = Keyboard.IsKeyToggled(Key.CapsLock) ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void HandlePasswordBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var fe = FindCapsLockIndicator((Control)sender);
            if (fe != null)
                fe.Visibility = Visibility.Collapsed;
        }

        private static FrameworkElement FindCapsLockIndicator(Control pb)
        {
            return pb?.Template?.FindName("PART_CapsLockIndicator", pb) as FrameworkElement;
        }

        public static void SetSecurePassword(PasswordBox element, SecureString secureString)
        {
            element.SetValue(SecurePasswordBindingProperty, secureString);
        }

        public static SecureString GetSecurePassword(PasswordBox element)
        {
            return element.GetValue(SecurePasswordBindingProperty) as SecureString;
        }

        private static void AttachedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // we'll need to hook up to one of the element events
            // in order to allow the GC to collect the control, we'll wrap the event handler inside an object living in an attached property
            // don't be tempted to use the Unloaded event as that will be fired  even when the control is still alive and well (e.g. switching tabs in a tab control) 
            var passwordBox = (PasswordBox)d;
            var bindingMarshaller = passwordBox.GetValue(PasswordBindingMarshallerProperty) as PasswordBindingMarshaller;

            if (bindingMarshaller == null)
            {
                bindingMarshaller = new PasswordBindingMarshaller(passwordBox);
                passwordBox.SetValue(PasswordBindingMarshallerProperty, bindingMarshaller);
            }

            bindingMarshaller.UpdatePasswordBox(e.NewValue as SecureString);
        }

        private class PasswordBindingMarshaller
        {
            private readonly PasswordBox _passwordBox;
            private bool _isMarshalling;

            public PasswordBindingMarshaller(PasswordBox passwordBox)
            {
                _passwordBox = passwordBox;
                _passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
            }

            public void UpdatePasswordBox(SecureString newPassword)
            {
                if (_isMarshalling)
                    return;

                _isMarshalling = true;

                try
                {
                    // setting up the SecuredPassword won't trigger a visual update so we'll have to use the Password property
                    //_passwordBox.Password = newPassword.ToUnsecuredString();

                    // you may try the statement below, however the benefits are minimal security wise (you still have to extract the unsecured password for copying)
                    if (newPassword != null)
                        newPassword.CopyInto(_passwordBox.SecurePassword);
                    else
                        _passwordBox.Clear();

                }
                finally
                {
                    _isMarshalling = false;
                }
            }

            private void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
            {
                // copy the password into the attached property
                if (_isMarshalling)
                    return;

                _isMarshalling = true;

                try
                {
                    SetSecurePassword(_passwordBox, _passwordBox.SecurePassword.Copy());
                }
                finally
                {
                    _isMarshalling = false;
                }
            }
        }
    }
}