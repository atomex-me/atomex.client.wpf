using System;
using System.Windows;
using Atomix.MarketData.Bitfinex;
using Atomix.Subsystems;
using Atomix.Client.Wpf.ViewModels;
using Atomix.Client.Wpf.Views;
using Atomix.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Atomix.Client.Wpf
{
    public partial class App
    {
        public const string DebugConfigFileName = "config.debug.json";
        public const string ProductionConfigFileName = "config.json";

        public static AtomixApp AtomixApp { get; private set; }

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
#if DEBUG
            .AddEmbeddedJsonFile(ResourceAssembly, DebugConfigFileName)
#else
            .AddEmbeddedJsonFile(ResourceAssembly, ProductionConfigFileName)
#endif
            //.AddJsonFile(DebugConfigFileName, optional: true, reloadOnChange: true)
            //.AddJsonFile(ProductionConfigFileName, optional: false, reloadOnChange: true)
            .Build();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // init logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Log.Information("Application startup");            

            // init Atomix client app
            AtomixApp = new AtomixApp(Configuration)
                .UseQuotesProvider(new BitfinexQuotesProvider(Currencies.Available, BitfinexQuotesProvider.Usd))
                .UseTerminal(new Terminal(Configuration));

            // init & show main view
            var mainView = new MainWindow();
            mainView.DataContext = new MainViewModel(mainView);
            mainView.Show();        
            mainView.ShowStartDialog(new StartViewModel(mainView));

            MainWindow = mainView;

            AtomixApp.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AtomixApp.Stop();

            Log.Information("Application shutdown");

            base.OnExit(e);
        }
    }
}