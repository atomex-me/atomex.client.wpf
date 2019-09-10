using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using Atomix.MarketData.Bitfinex;
using Atomix.Subsystems;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.ViewModels;
using Atomix.Client.Wpf.Views;
using Atomix.Common.Configuration;
using Atomix.Core;
using Atomix.Updates;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Atomix.Client.Wpf
{
    public partial class App
    {
        public static IAtomixApp AtomixApp { get; private set; }
        public static Updater Updater { get; private set; }

        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
#if DEBUG
            .AddEmbeddedJsonFile(ResourceAssembly, "config.debug.json")
#else
            .AddEmbeddedJsonFile(ResourceAssembly, "config.json")
#endif
            .Build();

        private static Assembly CoreAssembly { get; } = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Atomix.Client.Core");

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

            // ensure there are no other instances of the app
            //if (!SingleApp.TryStart("AtomixApp"))
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
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration, currenciesProvider);

            // init Atomix client app
            AtomixApp = new AtomixApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.MainNet),
                    baseCurrency: BitfinexQuotesProvider.Usd))
                .UseTerminal(new Terminal(Configuration));

            // init app updater
            Updater = new Updater()
                //.UseLocalBinariesProvider(@"Atomix.Client.Wpf.Installer.msi")
                //.UseLocalVersionProvider(@"version.json")
                .UseHttpMetadataProvider("https://atomix.me/versions.json", TargetPlatform.Windows)
                .UseMsiProductProvider("0779aaaa-2411-4948-b5bc-48f81b8c6143");

            // init & show main view
            var mainView = new MainWindow();
            mainView.DataContext = new MainViewModel(AtomixApp, mainView, mainView);
            mainView.Show();        
            mainView.ShowStartDialog(new StartViewModel(AtomixApp, mainView));

            MainWindow = mainView;

            AtomixApp.Start();
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

            AtomixApp.Stop();
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