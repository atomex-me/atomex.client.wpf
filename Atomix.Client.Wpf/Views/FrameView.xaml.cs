using System.Windows;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Client.Wpf.Views.SendViews;

namespace Atomix.Client.Wpf.Views
{
    public partial class FrameView : ChildView
    {
        public FrameView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var resolver = new PageResolver();
            resolver.AddResolver(Navigation.MessageAlias, () => new MessagePage());
            resolver.AddResolver(Navigation.SendAlias, () => new SendPage());
            resolver.AddResolver(Navigation.SendConfirmationAlias, () => new SendConfirmationPage());
            resolver.AddResolver(Navigation.ConversionConfirmationAlias, () => new ConversionConfirmationPage());
            resolver.AddResolver(Navigation.SendingAlias, () => new SendingPage());

            Navigation.UseResolver(resolver);
            Navigation.Service = Frame.NavigationService;
        }
    }
}