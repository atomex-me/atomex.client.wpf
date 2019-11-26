using System.Windows;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Views.SendViews;

namespace Atomex.Client.Wpf.Views
{
    public partial class FrameView : ChildWindow
    {
        public FrameView()
        {
            InitializeComponent();
        }

        private void OnLoaded(
            object sender,
            RoutedEventArgs e)
        {
            var resolver = new PageResolver();
            resolver.AddResolver(Navigation.MessageAlias, () => new MessagePage());
            resolver.AddResolver(Navigation.SendAlias, () => new SendPage());
            resolver.AddResolver(Navigation.SendConfirmationAlias, () => new SendConfirmationPage());
            resolver.AddResolver(Navigation.ConversionConfirmationAlias, () => new ConversionConfirmationPage());
            resolver.AddResolver(Navigation.SendingAlias, () => new SendingPage());
            resolver.AddResolver(Navigation.DelegateAlias, () => new DelegatePage());

            Navigation.UseResolver(resolver);
            Navigation.Service = Frame.NavigationService;
        }
    }
}