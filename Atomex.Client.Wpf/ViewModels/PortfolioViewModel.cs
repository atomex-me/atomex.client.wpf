using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Atomex.Common;
using Atomex.Subsystems;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.ViewModels.Abstract;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using PieSeries = OxyPlot.Series.PieSeries;

namespace Atomex.Client.Wpf.ViewModels
{
    public class PortfolioViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        public PlotModel PlotModel { get; set; }
        public IList<CurrencyViewModel> AllCurrencies { get; set; }
        private Color NoTokensColor { get; } = Color.FromArgb(50, 0, 0, 0);

        private decimal _portfolioValue;
        public decimal PortfolioValue
        {
            get => _portfolioValue;
            set { _portfolioValue = value; OnPropertyChanged(nameof(PortfolioValue)); }
        }

        public PortfolioViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public PortfolioViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.TerminalChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs e)
        {
            AllCurrencies = e.Terminal?.Account?.Currencies
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
            {
                series.Slices.Add(new PieSlice(Properties.Resources.PwNoTokens, 1) {Fill = NoTokensColor.ToOxyColor()});
                series.TrackerFormatString = "{1}";
            }

            PlotModel = new PlotModel {Culture = CultureInfo.InvariantCulture};
            PlotModel.Series.Add(series);

            OnPropertyChanged(nameof(PlotModel));

            if (PlotModel.PlotView != null) {
                PlotModel.PlotView.ActualController.UnbindMouseDown(OxyMouseButton.Left);
                PlotModel.PlotView.ActualController.BindMouseEnter(OxyPlot.PlotCommands.HoverSnapTrack);
            }
        }

        private void DesignerMode()
        {
            var random = new Random();

            AllCurrencies = DesignTime.Currencies
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