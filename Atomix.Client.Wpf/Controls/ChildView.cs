using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Atomix.Client.Wpf.Common;

namespace Atomix.Client.Wpf.Controls
{
    public class ChildView : ContentControl, IClosable
    {
        public static readonly DependencyProperty CloseByEscapeProperty = DependencyProperty.Register(nameof(CloseByEscape),
                typeof(bool),
                typeof(ChildView),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(nameof(ShowCloseButton),
                typeof(bool),
                typeof(ChildView),
                new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen),
                typeof(bool),
                typeof(ChildView),
                new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsOpenedChanged));

        public static readonly RoutedEvent IsOpenChangedEvent = EventManager.RegisterRoutedEvent(nameof(IsOpenChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ChildView));

        public event RoutedEventHandler IsOpenChanged
        {
            add => AddHandler(IsOpenChangedEvent, value);
            remove => RemoveHandler(IsOpenChangedEvent, value);
        }

        public event EventHandler<CancelEventArgs> Closing;

        public static readonly RoutedEvent ClosingFinishedEvent
            = EventManager.RegisterRoutedEvent(nameof(ClosingFinished),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ChildView));

        public event RoutedEventHandler ClosingFinished
        {
            add => AddHandler(ClosingFinishedEvent, value);
            remove => RemoveHandler(ClosingFinishedEvent, value);
        }

        public bool CloseByEscape
        {
            get => (bool)GetValue(CloseByEscapeProperty);
            set => SetValue(CloseByEscapeProperty, value);
        }

        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        private ICommand _closeButtonCommand;
        public ICommand CloseButtonCommand => _closeButtonCommand ?? (_closeButtonCommand = new Command(() => CloseView()));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public object ChildViewResult { get; set; }
        public bool HideOverlayWhenClose { get; set; } = true;

        private static void IsOpenedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(e.OldValue, e.NewValue))
                return;

            var childView = (ChildView)dependencyObject;

            void OpenedChangedAction()
            {
                if ((bool)e.NewValue)
                {
                    var parent = childView.Parent as Panel;
                    Panel.SetZIndex(childView, parent?.Children.Count + 1 ?? 99);

                    childView.TryFocusElement();
                }
                else
                {
                    childView.OnClosingFinished();
                }

                VisualStateManager.GoToState(childView, (bool) e.NewValue == false ? "Hide" : "Show", true);

                childView.RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, childView));
            }

            childView.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) OpenedChangedAction);
        }

        private void TryFocusElement()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
                Focus();
        }

        private void OnClosingFinished()
        {
            RaiseEvent(new RoutedEventArgs(ClosingFinishedEvent, this));
        }

        protected virtual void OnClosing(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        public bool CloseView(object childViewResult = null)
        {
            var e = new CancelEventArgs();
            OnClosing(e);

            if (!e.Cancel)
            {
                ChildViewResult = childViewResult;
                IsOpen = false;
                return true;
            }

            return false;
        }

        public void Close()
        {
            CloseView();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (CloseByEscape && e.Key == Key.Escape)
                e.Handled = CloseView();

            OnPreviewKeyUp(e);
        }
    }
}