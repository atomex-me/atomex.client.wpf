using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Atomix.Client.Wpf.Controls
{
    public class StepIndicatorPanel : FrameworkElement
    {
        public static DependencyProperty BackgroundProperty = DependencyProperty.Register("Background",
            typeof(Brush),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground",
            typeof(Brush),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty StepBackgroundProperty = DependencyProperty.Register("StepBackground",
            typeof(Brush),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1e, 0x56, 0xaf)), FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty CompletedStepBackgroundProperty = DependencyProperty.Register("CompletedStepBackground",
            typeof(Brush),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1a, 0x40, 0x70)), FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty StepsCountProperty = DependencyProperty.Register("StepsCount",
            typeof(int),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty CurrentStepProperty = DependencyProperty.Register("CurrentStep",
            typeof(int),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty StepEllipseRadiusProperty = DependencyProperty.Register("StepEllipseRadius",
            typeof(double),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty CompletedStepEllipseRadiusProperty = DependencyProperty.Register("CompletedStepEllipseRadius",
            typeof(double),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(17.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty StepLineWidthProperty = DependencyProperty.Register("StepLineWidth",
            typeof(double),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty CompletedStepLineWidthProperty = DependencyProperty.Register("CompletedStepLineWidth",
            typeof(double),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily",
            typeof(FontFamily),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(new FontFamily(new Uri("pack://application:,,,/resources/"), "./#Roboto Medium"), FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize",
            typeof(double),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(23.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle",
            typeof(FontStyle),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight",
            typeof(FontWeight),
            typeof(StepIndicatorPanel),
            new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DependencyProperty FontStretchProperty = DependencyProperty.Register("FontStretch",
            typeof(FontStretch),
            typeof(StepIndicatorPanel),
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

        public Brush StepBackground
        {
            get => (Brush)GetValue(StepBackgroundProperty);
            set => SetValue(StepBackgroundProperty, value);
        }

        public Brush CompletedStepBackground
        {
            get => (Brush)GetValue(CompletedStepBackgroundProperty);
            set => SetValue(CompletedStepBackgroundProperty, value);
        }

        public int StepsCount
        {
            get => (int)GetValue(StepsCountProperty);
            set => SetValue(StepsCountProperty, value);
        }

        public int CurrentStep
        {
            get => (int)GetValue(CurrentStepProperty);
            set => SetValue(CurrentStepProperty, value);
        }

        public double StepEllipseRadius
        {
            get => (double)GetValue(StepEllipseRadiusProperty);
            set => SetValue(StepEllipseRadiusProperty, value);
        }

        public double CompletedStepEllipseRadius
        {
            get => (double)GetValue(CompletedStepEllipseRadiusProperty);
            set => SetValue(CompletedStepEllipseRadiusProperty, value);
        }

        public double StepLineWidth
        {
            get => (double)GetValue(StepLineWidthProperty);
            set => SetValue(StepLineWidthProperty, value);
        }

        public double CompletedStepLineWidth
        {
            get => (double)GetValue(CompletedStepLineWidthProperty);
            set => SetValue(CompletedStepLineWidthProperty, value);
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
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;

            var lineAvailableWidth = bounds.X + bounds.Width - StepEllipseRadius * 2;
            var lineWidth = StepsCount > 1 ? lineAvailableWidth / (StepsCount - 1) : 0;
            var startX = StepsCount > 1 ? bounds.X + StepEllipseRadius : centerX;

            // draw background
            context.DrawRectangle(Background, null, bounds);

            // draw background line
            if (StepsCount > 1)
                context.DrawLine(
                    pen: new Pen(StepBackground, StepLineWidth),
                    point0: new Point(bounds.X + StepEllipseRadius, centerY),
                    point1: new Point(bounds.Right - StepEllipseRadius, centerY));

            // draw steps
            for (var i = 0; i < StepsCount; ++i)
            {
                var stepPoint = new Point(startX + lineWidth * i, centerY);

                context.DrawEllipse(
                    brush: StepBackground,
                    pen: null,
                    center: stepPoint,
                    radiusX: StepEllipseRadius,
                    radiusY: StepEllipseRadius);
            }

            // draw completed steps
            for (var i = 0; i <= CurrentStep && i < StepsCount; ++i)
            {
                var stepPoint = new Point(startX + lineWidth * i, centerY);
                var prevStepPoint = new Point(startX + lineWidth * (i > 0 ? i - 1 : 0), centerY);

                context.DrawEllipse(
                    brush: CompletedStepBackground,
                    pen: null,
                    center: stepPoint,
                    radiusX: CompletedStepEllipseRadius,
                    radiusY: CompletedStepEllipseRadius);

                context.DrawLine(
                    pen: new Pen(CompletedStepBackground, CompletedStepLineWidth),
                    point0: prevStepPoint,
                    point1: stepPoint);
            }

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

            // draw labels
            for (var i = 0; i < StepsCount; ++i)
            {
                var stepText = new FormattedText(
                    textToFormat: (i + 1).ToString(),
                    culture: CultureInfo.InvariantCulture,
                    flowDirection: FlowDirection.LeftToRight,
                    typeface: typeface,
                    emSize: FontSize,
                    foreground: Foreground);

                var stepPoint = new Point(startX + lineWidth * i - stepText.Width / 2, centerY - stepText.Height / 2);

                context.DrawText(stepText, stepPoint);
            }
        }
    }
}