using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using Atomex.MarketData.Bitfinex;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.ViewModels;
using Atomex.Client.Wpf.Views;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.Updates;
using Atomex.Subsystems;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Atomex.Client.Wpf
{
    public partial class App
    {
        public static IAtomexApp AtomexApp { get; private set; }
        public static Updater Updater { get; private set; }

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
#if DEBUG
            .AddEmbeddedJsonFile(ResourceAssembly, "config.debug.json")
#else
            .AddEmbeddedJsonFile(ResourceAssembly, "config.json")
#endif
            .Build();

        private static Assembly CoreAssembly { get; } = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

        private static IConfiguration CurrenciesConfiguration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEmbeddedJsonFile(CoreAssembly, "currencies.json")
            .Build();

        private static IConfiguration SymbolsConfiguration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEmbeddedJsonFile(CoreAssembly, "symbols.json")
            .Build();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
            //{
            //    Source = new Uri("Themes/blue.xaml", UriKind.Relative)
            //});

            // ensure there are no other instances of the app
            //if (!SingleApp.TryStart("AtomexApp"))
            //{
            //    Current.Shutdown(300);
            //    return;
            //}

            // init logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Log.Information("Application startup");

            var currenciesProvider = new CurrenciesProvider(CurrenciesConfiguration);
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration);

            // init Atomex client app
            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseCurrenciesUpdater(new CurrenciesUpdater(currenciesProvider))
                .UseSymbolsUpdater(new SymbolsUpdater(symbolsProvider))
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.MainNet),
                    baseCurrency: BitfinexQuotesProvider.Usd));

            // init app updater
            Updater = new Updater()
                //.UseLocalBinariesProvider(@"Atomex.Client.Wpf.Installer.msi")
                //.UseLocalVersionProvider(@"version.json")
                .UseHttpMetadataProvider("https://atomex.me/versions.json", TargetPlatform.Windows)
                .UseMsiProductProvider("DB7FCF8D-E0C6-4C99-A6B1-3FB6D703F97E");

            // init & show main view
            var mainView = new MainWindow();
            mainView.DataContext = new MainViewModel(AtomexApp, mainView, mainView);
            mainView.Show();        
            mainView.ShowDialog(Dialogs.Start, new StartViewModel(AtomexApp, mainView));

            MainWindow = mainView;

            AtomexApp.Start();
            try { Updater.Start(); }
            catch (TimeoutException) { Log.Error("Failed to start the updater due to timeout"); }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // the app is already running
            if (e.ApplicationExitCode == 300)
            {
                SingleApp.CloseAndSwitch();
                base.OnExit(e);
                return;
            }

            AtomexApp.Stop();
            try { Updater.Stop(); }
            catch (TimeoutException) { Log.Error("Failed to stop the updater due to timeout"); }

            // update has been requested
            if (e.ApplicationExitCode == 101)
            {
                try
                {
                    Updater.RunUpdate();
                    Log.Information("Update scheduled");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to schedule update");
                }
            }

            Log.Information("Application shutdown");

            SingleApp.Close();
            base.OnExit(e);
        }
    }
}