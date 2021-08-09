using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CefSharp;
using CefSharp.Wpf;

namespace Atomex.Client.Wpf.Controls
{
    public partial class WebBrowserWrapper : UserControl
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                name: nameof(Source),
                propertyType: typeof(string),
                ownerType: typeof(WebBrowserWrapper),
                typeMetadata: new PropertyMetadata(null, new PropertyChangedCallback((o, e) =>
                {
                    if (o is not WebBrowserWrapper webBrowserWrapper)
                        return;

                    if (e.NewValue is string uri && uri != null)
                    {
                        webBrowserWrapper.webBrowser.Address = uri;
                    }
                })));

        public static readonly DependencyProperty NavigationStartingProperty =
           DependencyProperty.Register(
               name: nameof(NavigationStarting),
               propertyType: typeof(ICommand),
               ownerType: typeof(WebBrowserWrapper),
               typeMetadata: new PropertyMetadata(null));

        public static readonly DependencyProperty NavigationStartingParameterProperty =
           DependencyProperty.Register(
               name: nameof(NavigationStartingParameter),
               propertyType: typeof(object),
               ownerType: typeof(WebBrowserWrapper),
               typeMetadata: new PropertyMetadata(null));

        public static readonly DependencyProperty NavigationCompletedProperty =
           DependencyProperty.Register(
               name: nameof(NavigationCompleted),
               propertyType: typeof(ICommand),
               ownerType: typeof(WebBrowserWrapper),
               typeMetadata: new PropertyMetadata(null));

        public static readonly DependencyProperty NavigationCompletedParameterProperty =
           DependencyProperty.Register(
               name: nameof(NavigationCompletedParameter),
               propertyType: typeof(object),
               ownerType: typeof(WebBrowserWrapper),
               typeMetadata: new PropertyMetadata(null));

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public ICommand NavigationStarting
        {
            get => (ICommand)GetValue(NavigationStartingProperty);
            set => SetValue(NavigationStartingProperty, value);
        }

        public object NavigationStartingParameter
        {
            get => GetValue(NavigationStartingParameterProperty);
            set => SetValue(NavigationStartingParameterProperty, value);
        }

        public ICommand NavigationCompleted
        {
            get => (ICommand)GetValue(NavigationCompletedProperty);
            set => SetValue(NavigationCompletedProperty, value);
        }

        public object NavigationCompletedParameter
        {
            get => GetValue(NavigationCompletedParameterProperty);
            set => SetValue(NavigationCompletedParameterProperty, value);
        }

        public WebBrowserWrapper()
        {
            var inited = Cef.Initialize(
                settings: new CefSettings(),
                performDependencyCheck: false,
                browserProcessHandler: null);

            InitializeComponent();

            webBrowser.LoadingStateChanged += WebBrowserLoadingStateChanged;
        }

        private void WebBrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading)
            {
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NavigationStarting?.Execute(NavigationStartingParameter);
                });
            }
            else
            {
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NavigationCompleted?.Execute(NavigationCompletedParameter);
                });
            }
        }
    }
}