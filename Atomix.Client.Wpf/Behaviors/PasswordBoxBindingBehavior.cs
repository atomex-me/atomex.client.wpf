using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interactivity;
using MahApps.Metro.Controls;

namespace Atomix.Client.Wpf.Behaviors
{
    public class PasswordBoxBindingBehavior : Behavior<PasswordBox>
    {
        public static readonly DependencyProperty PasswordProperty
            = DependencyProperty.RegisterAttached(
                "Password",
                typeof(string),
                typeof(PasswordBoxBindingBehavior),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

        public static string GetPassword(DependencyObject dpo)
        {
            return (string)dpo.GetValue(PasswordProperty);
        }

        public static void SetPassword(DependencyObject dpo, string value)
        {
            dpo.SetValue(PasswordProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox targetPasswordBox)
            {
                targetPasswordBox.PasswordChanged -= PasswordBoxPasswordChanged;

                if (!GetIsChanging(targetPasswordBox))
                    targetPasswordBox.Password = (string)e.NewValue;

                targetPasswordBox.PasswordChanged += PasswordBoxPasswordChanged;
            }
        }

        private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = (PasswordBox)sender;
            SetIsChanging(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsChanging(passwordBox, false);
        }

        private static void SetRevealedPasswordCaretIndex(PasswordBox passwordBox)
        {
            var textBox = GetRevealedPasswordTextBox(passwordBox);
            if (textBox != null)
            {
                var caretPos = GetPasswordBoxCaretPosition(passwordBox);
                textBox.CaretIndex = caretPos;
            }
        }

        private static int GetPasswordBoxCaretPosition(PasswordBox passwordBox)
        {
            var selection  = GetSelection(passwordBox);

            var textRange = selection?
                .GetType()
                .GetInterfaces()
                .FirstOrDefault(i => i.Name == "ITextRange");

            var start = textRange?
                .GetProperty("Start")?
                .GetGetMethod()?
                .Invoke(selection, null);

            var value = start?
                .GetType()
                .GetProperty("Offset", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(start, null) as int?;

            var caretPosition = value.GetValueOrDefault(0);
            return caretPosition;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PasswordChanged += PasswordBoxPasswordChanged;
            AssociatedObject.Loaded += PasswordBoxLoaded;

            var selection = GetSelection(AssociatedObject);
            if (selection != null)
                selection.Changed += PasswordBoxSelectionChanged;
        }

        private void PasswordBoxLoaded(object sender, RoutedEventArgs e)
        {
            SetPassword(AssociatedObject, AssociatedObject.Password);

            var textBox = AssociatedObject.FindChild<TextBox>("RevealedPassword");
            if (textBox != null)
            {
                var selection = GetSelection(AssociatedObject);
                if (selection == null)
                {
                    var infos = AssociatedObject.GetType().GetProperty("Selection", BindingFlags.NonPublic | BindingFlags.Instance);
                    selection = infos?.GetValue(AssociatedObject, null) as TextSelection;
                    SetSelection(AssociatedObject, selection);
                    if (selection != null)
                    {
                        SetRevealedPasswordTextBox(AssociatedObject, textBox);
                        SetRevealedPasswordCaretIndex(AssociatedObject);

                        selection.Changed += PasswordBoxSelectionChanged;
                    }
                }
            }
        }

        private void PasswordBoxSelectionChanged(object sender, EventArgs e)
        {
            SetRevealedPasswordCaretIndex(AssociatedObject);
        }

        /// <summary>
        ///     Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>
        ///     Override this to unhook functionality from the AssociatedObject.
        /// </remarks>
        protected override void OnDetaching()
        {
            // it seems, it was already detached, or never attached
            if (AssociatedObject != null)
            {
                var selection = GetSelection(AssociatedObject);
                if (selection != null)
                {
                    selection.Changed -= PasswordBoxSelectionChanged;
                }
                AssociatedObject.Loaded -= PasswordBoxLoaded;
                AssociatedObject.PasswordChanged -= PasswordBoxPasswordChanged;
            }
            base.OnDetaching();
        }

        private static readonly DependencyProperty IsChangingProperty
            = DependencyProperty.RegisterAttached("IsChanging",
                                                  typeof(bool),
                                                  typeof(PasswordBoxBindingBehavior),
                                                  new UIPropertyMetadata(false));

        private static bool GetIsChanging(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsChangingProperty);
        }

        private static void SetIsChanging(DependencyObject obj, bool value)
        {
            obj.SetValue(IsChangingProperty, value);
        }

        private static readonly DependencyProperty SelectionProperty
            = DependencyProperty.RegisterAttached("Selection",
                                                  typeof(TextSelection),
                                                  typeof(PasswordBoxBindingBehavior),
                                                  new UIPropertyMetadata(default(TextSelection)));

        private static TextSelection GetSelection(DependencyObject obj)
        {
            return (TextSelection)obj.GetValue(SelectionProperty);
        }

        private static void SetSelection(DependencyObject obj, TextSelection value)
        {
            obj.SetValue(SelectionProperty, value);
        }

        private static readonly DependencyProperty RevealedPasswordTextBoxProperty
            = DependencyProperty.RegisterAttached("RevealedPasswordTextBox",
                                                  typeof(TextBox),
                                                  typeof(PasswordBoxBindingBehavior),
                                                  new UIPropertyMetadata(default(TextBox)));

        private static TextBox GetRevealedPasswordTextBox(DependencyObject obj)
        {
            return (TextBox)obj.GetValue(RevealedPasswordTextBoxProperty);
        }

        private static void SetRevealedPasswordTextBox(DependencyObject obj, TextBox value)
        {
            obj.SetValue(RevealedPasswordTextBoxProperty, value);
        }
    }
}