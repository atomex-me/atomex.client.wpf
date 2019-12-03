using System.Windows.Controls;
using System.Windows.Navigation;

namespace Atomex.Client.Wpf.Common
{
    public class Navigation
    {
        public const string NotFoundAlias = "NotFoundPage";
        public const string SendAlias = "SendPage";
        public const string SendConfirmationAlias = "SendConfirmationPage";
        public const string DelegateConfirmationAlias = "DelegateConfirmationPage";
        public const string ConversionConfirmationAlias = "ConversionConfirmationPage";
        public const string SendingAlias = "SendingPage";
        public const string MessageAlias = "MessagePage";
        public const string DelegateAlias = "DelegatePage";

        private static volatile Navigation _instance;
        private static readonly object SyncRoot = new object();

        private IPageResolver _resolver;
        private NavigationService _service;

        private Navigation() { }

        private static Navigation Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new Navigation();
                }

                return _instance;
            }
        }

        public static NavigationService Service
        {
            get => Instance._service;
            set
            {
                if (Instance._service != null)
                    Instance._service.Navigated -= Instance.ServiceOnNavigated;

                Instance._service = value;
                Instance._service.Navigated += Instance.ServiceOnNavigated;
            }
        }

        private void ServiceOnNavigated(object sender, NavigationEventArgs e)
        {
            if (!(e.Content is Page page))
                return;

            if (e.ExtraData != null)
                page.DataContext = e.ExtraData;
        }

        public static void Navigate(Page page, object context)
        {
            if (Instance._service == null || page == null)
                return;

            Instance._service.Navigate(
                root: page,
                navigationState: context);
        }

        public static void Navigate(Page page)
        {
            Navigate(page, null);
        }

        public static void Navigate(string uri, object context)
        {
            if (Instance._service == null || uri == null)
                return;

            var page = Instance._resolver.GetPage(uri);

            Navigate(page, context);
        }

        public static void Navigate(string uri)
        {
            Navigate(uri, null);
        }

        public static void Back()
        {
            if (Instance._service.CanGoBack)
                Instance._service.GoBack();
        }

        public static void UseResolver(IPageResolver resolver)
        {
            Instance._resolver = resolver;
        }
    }
}