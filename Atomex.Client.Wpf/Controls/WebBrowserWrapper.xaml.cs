using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Microsoft.Web.WebView2.Core;

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
                        webBrowserWrapper.webBrowser.Source = new Uri(uri);
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
#if DEBUG
            if (Env.IsInDesignerMode())
                Source = "https://sandbox.wert.io?partner_id=01F298K3HP4DY326AH1NS3MM3M&theme=dark";
#endif

            InitializeComponent();

            webBrowser.NavigationStarting += NavigationStartingHandler;
            webBrowser.NavigationCompleted += NavigationCompletedHandler;
        }

        private void NavigationCompletedHandler(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            NavigationCompleted?.Execute(NavigationCompletedParameter);
        }

        private void NavigationStartingHandler(
            object sender,
            CoreWebView2NavigationStartingEventArgs e)
        {
            NavigationStarting?.Execute(NavigationStartingParameter);
        }
    }
}