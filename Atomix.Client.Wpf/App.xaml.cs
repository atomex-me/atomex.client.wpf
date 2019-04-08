using System;
using System.Windows;
using Atomix.MarketData.Bitfinex;
using Atomix.Subsystems;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.ViewModels;
using Atomix.Client.Wpf.Views;
using Atomix.Common.Configuration;
using Atomix.Updates;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Atomix.Client.Wpf
{
    public partial class App
    {
        public const string DebugConfigFileName = "config.debug.json";
        public const string ProductionConfigFileName = "config.json";

        public static AtomixApp AtomixApp { get; private set; }
        public static Updater Updater { get; private set; }

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

            // init app updater
            Updater = new Updater()
                //.UseLocalBinariesProvider(@"Atomix.Client.Wpf.Installer.msi")
                //.UseLocalVersionProvider(@"version.json")
                .UseHttpMetadataProvider("https://atomix.me/versions.json", TargetPlatform.Windows)
                .UseMsiProductProvider("0779aaaa-2411-4948-b5bc-48f81b8c6143");

            // init & show main view
            var mainView = new MainWindow();
            mainView.DataContext = new MainViewModel(mainView);
            mainView.Show();        
            mainView.ShowStartDialog(new StartViewModel(mainView));

            MainWindow = mainView;

            AtomixApp.Start();
            try { Updater.Start(); }
            catch (TimeoutException) { Log.Error("Failed to start the updater due to timeout"); }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AtomixApp.Stop();
            try { Updater.Stop(); }
            catch (TimeoutException) { Log.Error("Failed to stop the updater due to timeout"); }

            if (e.ApplicationExitCode == 101) // update has been requested
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

            base.OnExit(e);
        }
    }
}