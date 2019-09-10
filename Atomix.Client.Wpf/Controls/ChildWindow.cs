using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Atomix.Client.Wpf.Common;
using ControlzEx.Native;
using MahApps.Metro.Controls;

namespace Atomix.Client.Wpf.Controls
{
    [TemplatePart(Name = PART_Overlay, Type = typeof(Grid))]
    [TemplatePart(Name = PART_Window, Type = typeof(Grid))]
    [TemplatePart(Name = PART_Header, Type = typeof(Grid))]
    [TemplatePart(Name = PART_HeaderThumb, Type = typeof(UIElement))]
    [TemplatePart(Name = PART_Icon, Type = typeof(ContentControl))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_Border, Type = typeof(Border))]
    [TemplatePart(Name = PART_Content, Type = typeof(ContentPresenter))]
    public class ChildWindow : ContentControl
    {
        private const string PART_Overlay = "PART_Overlay";
        private const string HideStoryboard = "HideStoryboard";
        private const string PART_Window = "PART_Window";
        private const string PART_Header = "PART_Header";
        private const string PART_HeaderThumb = "PART_HeaderThumb";
        private const string PART_Icon = "PART_Icon";
        private const string PART_CloseButton = "PART_CloseButton";
        private const string PART_Border = "PART_Border";
        private const string PART_Content = "PART_Content";

        /// <summary>
        /// Identifies the <see cref="AllowMove"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowMoveProperty
            = DependencyProperty.Register(nameof(AllowMove),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(default(bool)));

        /// <summary>
        /// Identifies the <see cref="IsModal"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsModalProperty
            = DependencyProperty.Register(nameof(IsModal),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="OverlayBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OverlayBrushProperty
            = DependencyProperty.Register(nameof(OverlayBrush),
                                          typeof(Brush),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="CloseOnOverlay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseOnOverlayProperty
            = DependencyProperty.Register(nameof(CloseOnOverlay),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(default(bool)));

        /// <summary>
        /// Identifies the <see cref="CloseByEscape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseByEscapeProperty
            = DependencyProperty.Register(nameof(CloseByEscape),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ShowTitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTitleBarProperty
            = DependencyProperty.Register(nameof(ShowTitleBar),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="TitleBarHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarHeightProperty
            = DependencyProperty.Register(nameof(TitleBarHeight),
                                          typeof(int),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(30));

        /// <summary>
        /// Identifies the <see cref="TitleBarBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarBackgroundProperty
            = DependencyProperty.Register(nameof(TitleBarBackground),
                                          typeof(Brush),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="TitleBarNonActiveBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarNonActiveBackgroundProperty
            = DependencyProperty.Register(nameof(TitleBarNonActiveBackground),
                                          typeof(Brush),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(Brushes.Gray));

        /// <summary>
        /// Identifies the <see cref="NonActiveBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NonActiveBorderBrushProperty
            = DependencyProperty.Register(nameof(NonActiveBorderBrush),
                                          typeof(Brush),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(Brushes.Gray));

        /// <summary>
        /// Identifies the <see cref="TitleForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleForegroundProperty
            = DependencyProperty.Register(nameof(TitleForeground),
                                          typeof(Brush),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty
            = DependencyProperty.Register(nameof(Title),
                                          typeof(string),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(default(string)));

        /// <summary>
        /// Identifies the <see cref="TitleCharacterCasing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleCharacterCasingProperty
            = DependencyProperty.Register(nameof(TitleCharacterCasing),
                                          typeof(CharacterCasing),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(CharacterCasing.Normal, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure),
                                          value => CharacterCasing.Normal <= (CharacterCasing)value && (CharacterCasing)value <= CharacterCasing.Upper);

        /// <summary>
        /// Identifies the <see cref="TitleHorizontalAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleHorizontalAlignmentProperty
            = DependencyProperty.Register(nameof(TitleHorizontalAlignment),
                                          typeof(HorizontalAlignment),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(HorizontalAlignment.Stretch));

        /// <summary>
        /// Identifies the <see cref="TitleVerticalAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleVerticalAlignmentProperty
            = DependencyProperty.Register(nameof(TitleVerticalAlignment),
                                          typeof(VerticalAlignment),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(VerticalAlignment.Center));

        /// <summary>
        /// Identifies the <see cref="TitleTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleTemplateProperty
            = DependencyProperty.Register(nameof(TitleTemplate),
                                          typeof(DataTemplate),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="TitleFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontSizeProperty
            = DependencyProperty.Register(nameof(TitleFontSize),
                                          typeof(double),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(SystemFonts.CaptionFontSize, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="TitleFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontFamilyProperty
            = DependencyProperty.Register(nameof(TitleFontFamily),
                                          typeof(FontFamily),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(SystemFonts.CaptionFontFamily, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty
            = DependencyProperty.Register(nameof(Icon),
                                          typeof(object),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="IconTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconTemplateProperty
            = DependencyProperty.Register(nameof(IconTemplate),
                                          typeof(DataTemplate),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="ShowCloseButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCloseButtonProperty
            = DependencyProperty.Register(nameof(ShowCloseButton),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="CloseButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonStyleProperty
            = DependencyProperty.Register(nameof(CloseButtonStyle),
                                          typeof(Style),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="ClosingButtonCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClosingButtonCommandProperty
            = DependencyProperty.Register(nameof(ClosingButtonCommand),
                                          typeof(ICommand),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(default(ICommand)));

        /// <summary>
        /// Identifies the <see cref="ClosingButtonCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClosingButtonCommandParameterProperty
            = DependencyProperty.Register(nameof(ClosingButtonCommandParameter),
                                          typeof(object),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="IsOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty
            = DependencyProperty.Register(nameof(IsOpen),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsOpenedChanged));

        /// <summary>
        /// Identifies the <see cref="ChildWindowWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ChildWindowWidthProperty
            = DependencyProperty.Register(nameof(ChildWindowWidth),
                                          typeof(double),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure), IsWidthHeightValid);

        /// <summary>
        /// Identifies the <see cref="ChildWindowHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ChildWindowHeightProperty
            = DependencyProperty.Register(nameof(ChildWindowHeight),
                                          typeof(double),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure), IsWidthHeightValid);

        /// <summary>
        /// Identifies the <see cref="ChildWindowImage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ChildWindowImageProperty
            = DependencyProperty.Register(nameof(ChildWindowImage),
                                          typeof(MessageBoxImage),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(MessageBoxImage.None, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <see cref="EnableDropShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableDropShadowProperty
            = DependencyProperty.Register(nameof(EnableDropShadow),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="AllowFocusElement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowFocusElementProperty
            = DependencyProperty.Register(nameof(AllowFocusElement),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="FocusedElement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusedElementProperty
            = DependencyProperty.Register(nameof(FocusedElement),
                                          typeof(FrameworkElement),
                                          typeof(ChildWindow),
                                          new UIPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="GlowBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty
            = DependencyProperty.Register(nameof(GlowBrush),
                                          typeof(SolidColorBrush),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="NonActiveGlowBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NonActiveGlowBrushProperty
            = DependencyProperty.Register(nameof(NonActiveGlowBrush),
                                          typeof(SolidColorBrush),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(Brushes.Gray));

        /// <summary>
        /// Identifies the <see cref="IsAutoCloseEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAutoCloseEnabledProperty
            = DependencyProperty.Register(nameof(IsAutoCloseEnabled),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(false, IsAutoCloseEnabledChanged));

        /// <summary>
        /// Identifies the <see cref="AutoCloseInterval"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoCloseIntervalProperty
            = DependencyProperty.Register(nameof(AutoCloseInterval),
                                          typeof(long),
                                          typeof(ChildWindow),
                                          new FrameworkPropertyMetadata(5000L, AutoCloseIntervalChanged));

        /// <summary>
        /// Identifies the <see cref="IsWindowHostActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsWindowHostActiveProperty
            = DependencyProperty.Register(nameof(IsWindowHostActive),
                                          typeof(bool),
                                          typeof(ChildWindow),
                                          new PropertyMetadata(true));

        /// <summary>
        /// An event that will be raised when <see cref="IsOpen"/> dependency property changes.
        /// </summary>
        public static readonly RoutedEvent IsOpenChangedEvent
            = EventManager.RegisterRoutedEvent(nameof(IsOpenChanged),
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedEventHandler),
                                               typeof(ChildWindow));

        /// <summary>
        /// An event that will be raised when <see cref="IsOpen"/> dependency property changes.
        /// </summary>
        public event RoutedEventHandler IsOpenChanged
        {
            add => AddHandler(IsOpenChangedEvent, value);
            remove => RemoveHandler(IsOpenChangedEvent, value);
        }

        /// <summary>
        /// An event that will be raised when the ChildWindow is closing.
        /// </summary>
        public event EventHandler<CancelEventArgs> Closing;

        /// <summary>
        /// An event that will be raised when the closing animation has finished.
        /// </summary>
        public static readonly RoutedEvent ClosingFinishedEvent
            = EventManager.RegisterRoutedEvent(nameof(ClosingFinished),
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedEventHandler),
                                               typeof(ChildWindow));

        /// <summary>
        /// An event that will be raised when the closing animation has finished.
        /// </summary>
        public event RoutedEventHandler ClosingFinished
        {
            add => AddHandler(ClosingFinishedEvent, value);
            remove => RemoveHandler(ClosingFinishedEvent, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the child window can be moved inside the overlay container.
        /// </summary>
        public bool AllowMove
        {
            get => (bool)GetValue(AllowMoveProperty);
            set => SetValue(AllowMoveProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the child window is modal.
        /// </summary>
        public bool IsModal
        {
            get => (bool)GetValue(IsModalProperty);
            set => SetValue(IsModalProperty, value);
        }

        /// <summary>
        /// Gets or sets the overlay brush.
        /// </summary>
        public Brush OverlayBrush
        {
            get => (Brush)GetValue(OverlayBrushProperty);
            set => SetValue(OverlayBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the child window can be closed by clicking the overlay container.
        /// </summary>
        public bool CloseOnOverlay
        {
            get => (bool)GetValue(CloseOnOverlayProperty);
            set => SetValue(CloseOnOverlayProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the child window can be closed by the Escape key.
        /// </summary>
        public bool CloseByEscape
        {
            get => (bool)GetValue(CloseByEscapeProperty);
            set => SetValue(CloseByEscapeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the title bar is visible or not.
        /// </summary>
        public bool ShowTitleBar
        {
            get => (bool)GetValue(ShowTitleBarProperty);
            set => SetValue(ShowTitleBarProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the title bar.
        /// </summary>
        public int TitleBarHeight
        {
            get => (int)GetValue(TitleBarHeightProperty);
            set => SetValue(TitleBarHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the title bar background.
        /// </summary>
        public Brush TitleBarBackground
        {
            get => (Brush)GetValue(TitleBarBackgroundProperty);
            set => SetValue(TitleBarBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the title bar background for non-active status.
        /// </summary>
        public Brush TitleBarNonActiveBackground
        {
            get => (Brush)GetValue(TitleBarNonActiveBackgroundProperty);
            set => SetValue(TitleBarNonActiveBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush for non-active status.
        /// </summary>
        public Brush NonActiveBorderBrush
        {
            get => (Brush)GetValue(NonActiveBorderBrushProperty);
            set => SetValue(NonActiveBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the title foreground.
        /// </summary>
        public Brush TitleForeground
        {
            get => (Brush)GetValue(TitleForegroundProperty);
            set => SetValue(TitleForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the character casing of the title.
        /// </summary>
        public CharacterCasing TitleCharacterCasing
        {
            get => (CharacterCasing)GetValue(TitleCharacterCasingProperty);
            set => SetValue(TitleCharacterCasingProperty, value);
        }

        /// <summary>
        /// Gets or sets the title horizontal alignment.
        /// </summary>
        public HorizontalAlignment TitleHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(TitleHorizontalAlignmentProperty);
            set => SetValue(TitleHorizontalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the title vertical alignment.
        /// </summary>
        public VerticalAlignment TitleVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(TitleVerticalAlignmentProperty);
            set => SetValue(TitleVerticalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the title content template to show a custom title.
        /// </summary>
        public DataTemplate TitleTemplate
        {
            get => (DataTemplate)GetValue(TitleTemplateProperty);
            set => SetValue(TitleTemplateProperty, value);
        }

        /// <summary> 
        /// The FontSize property specifies the size of the title.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        public double TitleFontSize
        {
            get => (double)GetValue(TitleFontSizeProperty);
            set => SetValue(TitleFontSizeProperty, value);
        }

        /// <summary> 
        /// The FontFamily property specifies the font family of the title.
        /// </summary>
        [Bindable(true)]
        public FontFamily TitleFontFamily
        {
            get => (FontFamily)GetValue(TitleFontFamilyProperty);
            set => SetValue(TitleFontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets a icon for the title bar.
        /// </summary>
        [Bindable(true)]
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets a icon content template for the title bar.
        /// </summary>
        [Bindable(true)]
        public DataTemplate IconTemplate
        {
            get => (DataTemplate)GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets if the close button is visible.
        /// </summary>
        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets the close button style.
        /// </summary>
        [Bindable(true)]
        public Style CloseButtonStyle
        {
            get => (Style)GetValue(CloseButtonStyleProperty);
            set => SetValue(CloseButtonStyleProperty, value);
        }

        public event EventHandler CloseButtonClicked;

        private ICommand _closeButtonCommand;
        public ICommand CloseButtonCommand => _closeButtonCommand ?? (_closeButtonCommand = new Command(() =>
        {
            CloseButtonClicked?.Invoke(this, EventArgs.Empty);
            Close();
        }));

        /// <summary>
        /// Gets or sets the command that is executed when the Close Button is clicked.
        /// </summary>
        public ICommand ClosingButtonCommand
        {
            get => (ICommand)GetValue(ClosingButtonCommandProperty);
            set => SetValue(ClosingButtonCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the command parameter that is used by the CloseButtonCommand when the Close Button is clicked.
        /// </summary>
        public object ClosingButtonCommandParameter
        {
            get => GetValue(ClosingButtonCommandParameterProperty);
            set => SetValue(ClosingButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is open or closed.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void IsOpenedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(e.OldValue, e.NewValue))
                return;
     

            var childWindow = (ChildWindow)dependencyObject;

            void OpenedChangedAction()
            {
                if ((bool) e.NewValue)
                {
                    if (childWindow._hideStoryboard != null)
                    {
                        // don't let the storyboard end it's completed event
                        // otherwise it could be hidden on start
                        childWindow._hideStoryboard.Completed -= childWindow.HideStoryboard_Completed;
                    }

                    var parent = childWindow.Parent as Panel;
                    Panel.SetZIndex(childWindow, parent?.Children.Count + 1 ?? 99);

                    childWindow.TryFocusElement();

                    if (childWindow.IsAutoCloseEnabled)
                    {
                        childWindow.StartAutoCloseTimer();
                    }
                }
                else
                {
                    childWindow.StopAutoCloseTimer();

                    if (childWindow._hideStoryboard != null)
                    {
                        childWindow._hideStoryboard.Completed += childWindow.HideStoryboard_Completed;
                    }
                    else
                    {
                        childWindow.OnClosingFinished();
                    }
                }

                VisualStateManager.GoToState(childWindow, (bool) e.NewValue == false ? "Hide" : "Show", true);

                childWindow.RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, childWindow));
            }

            childWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) OpenedChangedAction);
        }

        private void TryFocusElement()
        {
            if (AllowFocusElement && !DesignerProperties.GetIsInDesignMode(this))
            {
                // first focus itself
                Focus();

                var elementToFocus = FocusedElement ?? this.FindChildren<UIElement>().FirstOrDefault(c => c.Focusable);
                if (ShowCloseButton && _closeButton != null && elementToFocus == null)
                {
                    _closeButton.Focusable = true;
                    elementToFocus = _closeButton;
                }

                if (elementToFocus != null)
                {
                    DependencyPropertyChangedEventHandler eh = null;
                    eh = (sender, args) => {
                        elementToFocus.IsVisibleChanged -= eh;
                        elementToFocus.Focus();
                    };
                    elementToFocus.IsVisibleChanged += eh;
                }
            }
        }

        private void HideStoryboard_Completed(object sender, EventArgs e)
        {
            _hideStoryboard.Completed -= HideStoryboard_Completed;
            OnClosingFinished();
        }

        private void OnClosingFinished()
        {
            RaiseEvent(new RoutedEventArgs(ClosingFinishedEvent, this));
        }

        /// <summary>
        /// Gets or sets the width of the child window.
        /// </summary>
        public double ChildWindowWidth
        {
            get => (double)GetValue(ChildWindowWidthProperty);
            set => SetValue(ChildWindowWidthProperty, value);
        }

        private static bool IsWidthHeightValid(object value)
        {
            var v = (double)value;
            return (double.IsNaN(v)) || (v >= 0.0d && !double.IsPositiveInfinity(v));
        }

        /// <summary>
        /// Gets or sets the height of the child window.
        /// </summary>
        public double ChildWindowHeight
        {
            get => (double)GetValue(ChildWindowHeightProperty);
            set => SetValue(ChildWindowHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets which image is shown on the left side of the window content.
        /// </summary>
        public MessageBoxImage ChildWindowImage
        {
            get => (MessageBoxImage)GetValue(ChildWindowImageProperty);
            set => SetValue(ChildWindowImageProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window has a drop shadow (glow brush).
        /// </summary>
        public bool EnableDropShadow
        {
            get => (bool)GetValue(EnableDropShadowProperty);
            set => SetValue(EnableDropShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the child window should try focus an element.
        /// </summary>
        public bool AllowFocusElement
        {
            get => (bool)GetValue(AllowFocusElementProperty);
            set => SetValue(AllowFocusElementProperty, value);
        }

        /// <summary>
        /// Gets or sets the focused element.
        /// </summary>
        public FrameworkElement FocusedElement
        {
            get => (FrameworkElement)GetValue(FocusedElementProperty);
            set => SetValue(FocusedElementProperty, value);
        }

        /// <summary>
        /// Gets or sets the glow brush (drop shadow).
        /// </summary>
        public SolidColorBrush GlowBrush
        {
            get => (SolidColorBrush)GetValue(GlowBrushProperty);
            set => SetValue(GlowBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the glow brush (drop shadow) for non-active status.
        /// </summary>
        public SolidColorBrush NonActiveGlowBrush
        {
            get => (SolidColorBrush)GetValue(NonActiveGlowBrushProperty);
            set => SetValue(NonActiveGlowBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ChildWindow should auto close after AutoCloseInterval has passed.
        /// </summary>
        public bool IsAutoCloseEnabled
        {
            get => (bool)GetValue(IsAutoCloseEnabledProperty);
            set => SetValue(IsAutoCloseEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the time in milliseconds when the ChildWindow should auto close.
        /// </summary>
        public long AutoCloseInterval
        {
            get => (long)GetValue(AutoCloseIntervalProperty);
            set => SetValue(AutoCloseIntervalProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the host Window is active or not.
        /// </summary>
        public bool IsWindowHostActive
        {
            get => (bool)GetValue(IsWindowHostActiveProperty);
            set => SetValue(IsWindowHostActiveProperty, value);
        }

        /// <summary>
        /// Gets the child window result when the dialog will be closed.
        /// </summary>
        public object ChildWindowResult { get; protected set; }

        private DispatcherTimer _autoCloseTimer;

        private static void IsAutoCloseEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = (ChildWindow)dependencyObject;

            void AutoCloseEnabledChangedAction()
            {
                if (e.NewValue != e.OldValue)
                {
                    if ((bool) e.NewValue)
                    {
                        if (childWindow.IsOpen)
                            childWindow.StartAutoCloseTimer();
                    }
                    else
                    {
                        childWindow.StopAutoCloseTimer();
                    }
                }
            }

            childWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) AutoCloseEnabledChangedAction);
        }

        private static void AutoCloseIntervalChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = (ChildWindow)dependencyObject;

            void AutoCloseIntervalChangedAction()
            {
                if (e.NewValue != e.OldValue)
                {
                    childWindow.InitializeAutoCloseTimer();

                    if (childWindow.IsAutoCloseEnabled && childWindow.IsOpen)
                        childWindow.StartAutoCloseTimer();
                }
            }

            childWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) AutoCloseIntervalChangedAction);
        }

        private void InitializeAutoCloseTimer()
        {
            StopAutoCloseTimer();

            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Tick += AutoCloseTimerCallback;
            _autoCloseTimer.Interval = TimeSpan.FromMilliseconds(AutoCloseInterval);
        }

        private void AutoCloseTimerCallback(object sender, EventArgs e)
        {
            StopAutoCloseTimer();

            // if the ChildWindow is open and autoclose is still enabled then close the ChildWindow
            if (IsOpen && IsAutoCloseEnabled)
                Close();
        }

        private void StartAutoCloseTimer()
        {
            // in case it is already running
            StopAutoCloseTimer();

            if (!DesignerProperties.GetIsInDesignMode(this))
                _autoCloseTimer.Start();
            
        }

        private void StopAutoCloseTimer()
        {
            if (_autoCloseTimer != null && _autoCloseTimer.IsEnabled)
                _autoCloseTimer.Stop();
        }

        private string _closeText;

        /// <summary>
        /// Gets the close button tool tip.
        /// </summary>
        public string CloseButtonToolTip
        {
            get
            {
                if (string.IsNullOrEmpty(_closeText))
                    _closeText = GetCaption(905);

                return _closeText;
            }
        }

        private Storyboard _hideStoryboard;
        private IMetroThumb _headerThumb;
        private Button _closeButton;
        private readonly TranslateTransform _moveTransform = new TranslateTransform();
        private Grid _partWindow;
        private Grid _partOverlay;
        private ContentControl _icon;

        static ChildWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                forType: typeof(ChildWindow),
                typeMetadata: new FrameworkPropertyMetadata(typeof(ChildWindow)));
        }

        public ChildWindow()
        {
            InitializeAutoCloseTimer();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // really necessary?
            if (Template == null)
                return;

            var isActiveBindingAction = new Action(() => {
                var window = Window.GetWindow(this);
                if (window != null)
                    SetBinding(IsWindowHostActiveProperty, new Binding(nameof(Window.IsActive)) { Source = window, Mode = BindingMode.OneWay });
            });

            if (!IsLoaded)
            {
                this.BeginInvoke(isActiveBindingAction, DispatcherPriority.Loaded);
            }
            else
            {
                isActiveBindingAction();
            }

            _hideStoryboard = Template.FindName(HideStoryboard, this) as Storyboard;

            if (_partOverlay != null)          
                _partOverlay.MouseLeftButtonDown -= PartOverlayOnClose;


            _partOverlay = Template.FindName(PART_Overlay, this) as Grid;
            if (_partOverlay != null)
                _partOverlay.MouseLeftButtonDown += PartOverlayOnClose;

            _partWindow = Template.FindName(PART_Window, this) as Grid;
            if (_partWindow != null)
                _partWindow.RenderTransform = _moveTransform;

            _icon = Template.FindName(PART_Icon, this) as ContentControl;

            if (_headerThumb != null)
                _headerThumb.DragDelta -= HeaderThumbDragDelta;

            _headerThumb = Template.FindName(PART_HeaderThumb, this) as IMetroThumb;
            if (_headerThumb != null && _partWindow != null)
                _headerThumb.DragDelta += HeaderThumbDragDelta;

            if (_closeButton != null)
                _closeButton.Click -= OnCloseButtonClick;

            _closeButton = Template.FindName(PART_CloseButton, this) as Button;
            if (_closeButton != null)
                _closeButton.Click += OnCloseButtonClick;
        }

        private void PartOverlayOnClose(object sender, MouseButtonEventArgs e)
        {
            if (Equals(e.OriginalSource, _partOverlay) && CloseOnOverlay)
                Close();
        }

        private void HeaderThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            var allowDragging = AllowMove && _partWindow.HorizontalAlignment != HorizontalAlignment.Stretch && _partWindow.VerticalAlignment != VerticalAlignment.Stretch;
            // drag only if IsWindowDraggable is set to true
            if (allowDragging && (Math.Abs(e.HorizontalChange) > 2 || Math.Abs(e.VerticalChange) > 2))
                ProcessMove(e.HorizontalChange, e.VerticalChange);
        }

        private void ProcessMove(double x, double y)
        {
            var width = _partOverlay.RenderSize.Width;
            var height = _partOverlay.RenderSize.Height;

            var widthOffset = width / 2 - _partWindow.RenderSize.Width / 2;
            var heightOffset = height / 2 - _partWindow.RenderSize.Height / 2;

            var realX = _moveTransform.X + x + widthOffset;
            var realY = _moveTransform.Y + y + heightOffset;

            const int extraGap = 5;
            var widthGap = Math.Max(_icon?.ActualWidth + 5 ?? 30, 30);
            var heightGap = Math.Max(TitleBarHeight, 30);
            var changeX = _moveTransform.X;
            var changeY = _moveTransform.Y;

            if (realX < (0 + extraGap))
            {
                changeX = -widthOffset + extraGap;
            }
            else if (realX > (width - widthGap - extraGap))
            {
                changeX = width - widthOffset - widthGap - extraGap;
            }
            else
            {
                changeX += x;
            }

            if (realY < (0 + extraGap))
            {
                changeY = -heightOffset + extraGap;
            }
            else if (realY > (height - heightGap - extraGap))
            {
                changeY = height - heightOffset - heightGap - extraGap;
            }
            else
            {
                changeY += y;
            }

            if (!Equals(changeX, _moveTransform.X) || !Equals(changeY, _moveTransform.Y))
            {
                _moveTransform.X = changeX;
                _moveTransform.Y = changeY;
                InvalidateArrange();
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// This method fires the <see cref="Closing"/> event.
        /// </summary>
        /// <param name="e">The EventArgs for the closing step.</param>
        protected virtual void OnClosing(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public bool Close(object childWindowResult = null)
        {
            // check if we really want close the dialog
            var e = new CancelEventArgs();
            OnClosing(e);
            if (!e.Cancel)
            {
                // now handle the command
                if (ClosingButtonCommand != null)
                {
                    var parameter = ClosingButtonCommandParameter ?? this;
                    if (!ClosingButtonCommand.CanExecute(parameter))
                        return false;

                    ClosingButtonCommand.Execute(parameter);
                }

                ChildWindowResult = childWindowResult;
                IsOpen = false;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (CloseByEscape && e.Key == Key.Escape)
                e.Handled = Close();

            OnPreviewKeyUp(e);
        }

#pragma warning disable 618
        private SafeLibraryHandle _user32;
#pragma warning restore 618

#pragma warning disable 618
        private string GetCaption(int id)
        {
            if (_user32 == null)
                _user32 = UnsafeNativeMethods.LoadLibrary(Environment.SystemDirectory + "\\User32.dll");

            var sb = new StringBuilder(256);
            UnsafeNativeMethods.LoadString(_user32, (uint)id, sb, sb.Capacity);
            return sb.ToString().Replace("&", "");
        }
#pragma warning restore 618
    }
}