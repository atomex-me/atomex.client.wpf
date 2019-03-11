using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Atomix.Client.Wpf.Controls
{
    public class PasswordStrengthMeter : FrameworkElement
    {
        public const int MaxPasswordScore = 5;
        public static Color WeakColor = Color.FromRgb(0xd9, 0x53, 0x4f);
        public static Color MediumColor = Color.FromRgb(0xf0, 0xad, 0x4e);
        public static Color StrongColor = Color.FromRgb(0x5c, 0xb8, 0x5c);

        public static DependencyProperty BackgroundProperty = DependencyProperty.Register("Background",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(Brushes.Transparent
                , FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(Brushes.White
                , FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty BlankBackgroundProperty = DependencyProperty.Register("BlankBackground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty TooShortBackgroundProperty = DependencyProperty.Register("TooShortBackground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(new SolidColorBrush(WeakColor),
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty WeakBackgroundProperty = DependencyProperty.Register("WeakBackground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(new SolidColorBrush(WeakColor),
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty MediumBackgroundProperty = DependencyProperty.Register("MediumBackground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(new SolidColorBrush(MediumColor),
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty StrongBackgroundProperty = DependencyProperty.Register("StrongBackground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(new SolidColorBrush(StrongColor),
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty VeryStrongBackgroundProperty = DependencyProperty.Register("VeryStrongBackground",
            typeof(Brush),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(new SolidColorBrush(StrongColor),
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty PasswordScoreProperty = DependencyProperty.Register("PasswordScore",
            typeof(int),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius",
            typeof(double),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty ShowCaptionProperty = DependencyProperty.Register("ShowCaption",
            typeof(bool),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily",
            typeof(FontFamily),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(new FontFamily(new Uri("pack://application:,,,/Resources/"),"./#Roboto"), FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize",
            typeof(double),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(11.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle",
            typeof(FontStyle),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight",
            typeof(FontWeight),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontStretchProperty = DependencyProperty.Register("FontStretch",
            typeof(FontStretch),
            typeof(PasswordStrengthMeter),
            new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush) GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public Brush BlankBackground
        {
            get => (Brush)GetValue(BlankBackgroundProperty);
            set => SetValue(BlankBackgroundProperty, value);
        }

        public Brush TooShortBackground
        {
            get => (Brush) GetValue(TooShortBackgroundProperty);
            set => SetValue(TooShortBackgroundProperty, value);
        }

        public Brush WeakBackground
        {
            get => (Brush) GetValue(WeakBackgroundProperty);
            set => SetValue(WeakBackgroundProperty, value);
        }

        public Brush MediumBackground
        {
            get => (Brush) GetValue(MediumBackgroundProperty);
            set => SetValue(MediumBackgroundProperty, value);
        }

        public Brush StrongBackground
        {
            get => (Brush) GetValue(StrongBackgroundProperty);
            set => SetValue(StrongBackgroundProperty, value);
        }

        public Brush VeryStrongBackground
        {
            get => (Brush) GetValue(VeryStrongBackgroundProperty);
            set => SetValue(VeryStrongBackgroundProperty, value);
        }

        public int PasswordScore
        {
            get => (int)GetValue(PasswordScoreProperty);
            set => SetValue(PasswordScoreProperty, value);
        }

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public bool ShowCaption
        {
            get => (bool)GetValue(ShowCaptionProperty);
            set => SetValue(ShowCaptionProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);

            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);

            context.DrawRectangle(Background, null, bounds);
            context.DrawRoundedRectangle(BlankBackground, null, bounds, CornerRadius, CornerRadius);

            var passwordScore = PasswordScore >= 0
                ? PasswordScore < MaxPasswordScore
                    ? PasswordScore
                    : MaxPasswordScore
                : 0;

            context.DrawRoundedRectangle(
                brush: ScoreToBrush(passwordScore),
                pen: null,
                rectangle: new Rect(0, 0, passwordScore * ActualWidth / 5, ActualHeight),
                radiusX: CornerRadius,
                radiusY: CornerRadius);

            if (ShowCaption)
            {
                var formattedText = new FormattedText(
                    textToFormat: ScoreToString(passwordScore),
                    culture: CultureInfo.InvariantCulture,
                    flowDirection: FlowDirection.LeftToRight,
                    typeface: new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                    emSize: FontSize,
                    foreground: Foreground);

                context.DrawText(
                    formattedText: formattedText,
                    origin: new Point(bounds.X + bounds.Width / 2 - formattedText.Width / 2,
                        bounds.Y + bounds.Height / 2 - formattedText.Height / 2));
            }
        }

        private Brush ScoreToBrush(int passwordScore)
        {
            if (passwordScore <= 0)
                return BlankBackground;
            if (passwordScore == 1)
                return TooShortBackground;
            if (passwordScore == 2)
                return WeakBackground;
            if (passwordScore == 3)
                return MediumBackground;
            if (passwordScore == 4)
                return StrongBackground;

            return VeryStrongBackground;     
        }

        // todo: localization
        private static string ScoreToString(int passwordScore)
        {
            switch (passwordScore)
            {
                case 0:
                    return "Blank";
                case 1:
                    return "Too Short";
                case 2:
                    return "Weak";
                case 3:
                    return "Medium";
                case 4:
                    return "Strong";
                case 5:
                    return "Very Strong";
                default:
                    return "Blank";
            }
        }
    }
}