using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Atomix.Common;
using Atomix.Subsystems;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.ViewModels.Abstract;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using PieSeries = OxyPlot.Series.PieSeries;

namespace Atomix.Client.Wpf.ViewModels
{
    public class PortfolioViewModel : BaseViewModel
    {
        public const string Nothing = "Nothing";

        public PlotModel PlotModel { get; set; }
        public IList<CurrencyViewModel> AllCurrencies { get; set; }
        public Color NothingColor { get; set; } = Color.FromArgb(50, 0, 0, 0);

        private decimal _portfolioValue;
        public decimal PortfolioValue
        {
            get => _portfolioValue;
            set { _portfolioValue = value; OnPropertyChanged(nameof(PortfolioValue)); }
        }

        public PortfolioViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode()) {
                DesignerMode();
                return;
            }
#endif

            SubscribeToServices(App.AtomixApp);
        }

        private void SubscribeToServices(AtomixApp app)
        {
            app.AccountChanged += OnAccountChangedEventHandler;
        }

        private void OnAccountChangedEventHandler(object sender, AccountChangedEventArgs e)
        {
            AllCurrencies = e.NewAccount?.Wallet.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c);
                    vm.AmountUpdated += OnAmountUpdatedEventHandler;
                    return vm;
                })
                .ToList() ?? new List<CurrencyViewModel>();

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            // update total portfolio value
            PortfolioValue = AllCurrencies.Sum(c => c.TotalAmountInBase);

            // update currency portfolio percent
            AllCurrencies.ForEachDo(c => c.PortfolioPercent = PortfolioValue != 0
                ? c.TotalAmountInBase / PortfolioValue
                : 0);
            OnPropertyChanged(nameof(AllCurrencies));

            UpdatePlotModel();
        }

        private void UpdatePlotModel()
        {
            var series = new PieSeries
            {
                StrokeThickness = 0,
                StartAngle = 0,
                AngleSpan = 360,
                //ExplodedDistance = 0.1,
                InnerDiameter = 0.6,
                TickHorizontalLength = 0,
                TickRadialLength = 0,
                OutsideLabelFormat = string.Empty,
                InsideLabelFormat = string.Empty,
                TrackerFormatString = "{1}: ${2:0.00} ({3:P2})"
            };

            foreach (var currency in AllCurrencies)
            {
                series.Slices.Add(
                    new PieSlice(currency.Currency.Name, (double)currency.TotalAmountInBase) {
                        Fill = currency.AccentColor.ToOxyColor()
                    });
            }

            if (PortfolioValue == 0)
                series.Slices.Add(new PieSlice(Nothing, 100) {Fill = NothingColor.ToOxyColor()});

            PlotModel = new PlotModel() {Culture = CultureInfo.InvariantCulture};
            PlotModel.Series.Add(series);
            OnPropertyChanged(nameof(PlotModel));
        }

        private void DesignerMode()
        {
            var random = new Random();

            AllCurrencies = Currencies.Available
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false);
                    vm.TotalAmountInBase = random.Next(100);
                    return vm;
                })
                .ToList();

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }
    }
}