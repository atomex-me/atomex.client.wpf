using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Atomix.Client.Wpf.Behaviors
{
    public class TextBoxInputBehavior : Behavior<TextBox>
    {
        private const NumberStyles ValidNumberStyles = NumberStyles.AllowDecimalPoint |
                                                       NumberStyles.AllowThousands |
                                                       NumberStyles.AllowLeadingSign;
        public TextBoxInputBehavior()
        {
            InputMode = TextBoxInputMode.None;
            JustPositiveDecimalInput = false;
        }

        public TextBoxInputMode InputMode { get; set; }
        public bool UseInvariantCulture { get; set; }

        public static readonly DependencyProperty JustPositiveDecimalInputProperty =
           DependencyProperty.Register(
               name: nameof(JustPositiveDecimalInput),
               propertyType: typeof(bool),
               ownerType: typeof(TextBoxInputBehavior),
               typeMetadata: new FrameworkPropertyMetadata(false));

        public bool JustPositiveDecimalInput
        {
            get => (bool) GetValue(JustPositiveDecimalInputProperty);
            set => SetValue(JustPositiveDecimalInputProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewTextInput += AssociatedObjectPreviewTextInput;
            AssociatedObject.PreviewKeyDown += AssociatedObjectPreviewKeyDown;

            DataObject.AddPastingHandler(AssociatedObject, Pasting);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewTextInput -= AssociatedObjectPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= AssociatedObjectPreviewKeyDown;

            DataObject.RemovePastingHandler(AssociatedObject, Pasting);
        }

        private void Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                if (!IsValidInput(GetText(pastedText)))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void AssociatedObjectPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && !IsValidInput(GetText(" ")))
                e.Handled = true;
        }

        private void AssociatedObjectPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsValidInput(GetText(e.Text)))
                e.Handled = true;
        }

        private string GetText(string input)
        {
            var txt = AssociatedObject;

            var selectionStart = txt.SelectionStart;
            if (txt.Text.Length < selectionStart)
                selectionStart = txt.Text.Length;

            var selectionLength = txt.SelectionLength;
            if (txt.Text.Length < selectionStart + selectionLength)
                selectionLength = txt.Text.Length - selectionStart;

            var realText = txt.Text.Remove(selectionStart, selectionLength);

            var caretIndex = txt.CaretIndex;
            if (realText.Length < caretIndex)
                caretIndex = realText.Length;

            var newText = realText.Insert(caretIndex, input);

            return newText;
        }

        private bool IsValidInput(string input)
        {
            switch (InputMode)
            {
                case TextBoxInputMode.None:
                    return true;

                case TextBoxInputMode.DigitInput:
                    return CheckIsDigit(input);

                case TextBoxInputMode.DecimalInput:
                    var culture = UseInvariantCulture
                            ? CultureInfo.InvariantCulture
                            : CultureInfo.CurrentCulture;

                    if (Regex.Matches(input, Regex.Escape(culture.NumberFormat.NumberDecimalSeparator)).Count > 1)
                        return false;

                    if (input.Contains("-"))
                    {
                        if (JustPositiveDecimalInput)
                            return false;

                        if (input.IndexOf("-", StringComparison.Ordinal) > 0)
                            return false;

                        if (input.ToCharArray().Count(x => x == '-') > 1)
                            return false;

                        if (input.Length == 1)
                            return true;
                    }                

                    return decimal.TryParse(input, ValidNumberStyles, culture, out _);

                default: throw new ArgumentException("Unknown TextBoxInputMode");
            }
        }

        private static bool CheckIsDigit(string input)
        {
            return input
                .ToCharArray()
                .All(char.IsDigit);
        }
    }

    public enum TextBoxInputMode
    {
        None,
        DecimalInput,
        DigitInput
    }
}