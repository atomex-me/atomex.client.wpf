using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using Atomex.Core.Entities;
using Atomex.Client.Wpf.Common;
using Atomex.Common;
using Atomex.Subsystems.Abstract;
using Atomex.Subsystems;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using DateTimeAxis = OxyPlot.Axes.DateTimeAxis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using System.Windows;

namespace Atomex.Client.Wpf.ViewModels
{
    public class SymbolTabItem : BaseViewModel
    {
        public Symbol Symbol { get; }
        public string Header => Symbol.Name;
        public CandleStickSeries CandleStickSeries;
        public LineAnnotation BidLineAnnotation;
        public LineAnnotation AskLineAnnotation;
        public PlotModel Model { get; set; }
        public DateTimeAxis XAxis { get; set; }
        public string XAxisFormat { get; } = "dd MMM HH:mm";
        public bool XAutoScroll { get; } = true;
        public double YMinMaxOffsetPercent { get; } = 0.2;

        public OxyColor PlotAreaBorderColor { get; } = OxyColors.Gray;
        public OxyColor TextColor { get; } = OxyColors.WhiteSmoke;
        public OxyColor TickLineColor { get; } = OxyColors.WhiteSmoke;
        public OxyColor AxisLineColor { get; } = OxyColors.WhiteSmoke;
        public OxyColor MajorGridLineColor { get; } = OxyColor.FromRgb(r: 31, g: 52, b: 86);
        public OxyColor IncreasingColor { get; } = OxyColor.FromRgb(r: 232, g: 236, b: 255);
        public OxyColor DecreasingColor { get; } = OxyColor.FromRgb(r: 85, g: 132, b: 196);

        public OxyColor BidLineColor { get; } = OxyColor.FromRgb(r: 85, g: 132, b: 196);
        public OxyColor AskLineColor { get; } = OxyColors.AliceBlue;

        public ObservableCollection<HighLowItem> Candles = new ObservableCollection<HighLowItem>();

        public SymbolTabItem(Symbol symbol)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));

            Model = new PlotModel();

            XAxis = new DateTimeAxis
            {
                TextColor = TextColor,
                StringFormat = XAxisFormat,
                TicklineColor = TickLineColor,
                AxislineColor = AxisLineColor,
                AxislineStyle = LineStyle.Solid,
                MajorGridlineColor = MajorGridLineColor,
                MajorGridlineStyle = LineStyle.Dash,
                MajorStep = PeriodToX(TimeSpan.FromMinutes(10)),             
                Position = AxisPosition.Bottom,
                IsZoomEnabled = false,
                MaximumRange = PeriodToX(TimeSpan.FromHours(1.5)),   
                MinimumRange = PeriodToX(TimeSpan.FromHours(1))
            };

            Model.Axes.Add(XAxis);

            var yAxis = new LinearAxis
            {
                TextColor = TextColor,
                TicklineColor = TickLineColor,
                AxislineColor = AxisLineColor,
                AxislineStyle = LineStyle.Solid,
                MajorGridlineColor = MajorGridLineColor,
                MajorGridlineStyle = LineStyle.Dash,
                Position = AxisPosition.Right
            };

            Model.Axes.Add(yAxis);

            Model.PlotAreaBorderThickness = new OxyThickness(thickness: 0);
            Model.PlotAreaBorderColor = PlotAreaBorderColor;

            CandleStickSeries = new CandleStickSeries
            {
                IncreasingColor = IncreasingColor,
                DecreasingColor = DecreasingColor,
                ItemsSource = Candles,
                CandleWidth = PeriodToX(TimeSpan.FromMinutes(1))
            };

            XAxis.AxisChanged += (sender, args) => { AdjustYExtent(CandleStickSeries, XAxis, yAxis); };

            Model.Series.Add(CandleStickSeries);

            BidLineAnnotation = new LineAnnotation
            {
                StrokeThickness = 1,
                Color = BidLineColor,
                Type = LineAnnotationType.Horizontal,
                Text = 0.0m.ToString(CultureInfo.InvariantCulture),
                TextLinePosition = 1, 
                TextColor = BidLineColor,
                X = 0,
                Y = 0
            };

            AskLineAnnotation = new LineAnnotation
            {
                StrokeThickness = 1,
                Color = AskLineColor,
                Type = LineAnnotationType.Horizontal,
                Text = 0.0m.ToString(CultureInfo.InvariantCulture),
                TextLinePosition = 1, 
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                TextColor = AskLineColor,
                X = 0,
                Y = 0
            };

            Model.Annotations.Add(BidLineAnnotation);
            Model.Annotations.Add(AskLineAnnotation);
            Model.Updated += async (sender, args) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Model.PlotView != null)
                        Model.PlotView?.ActualController.Unbind(PlotCommands.ZoomRectangle);
                }, DispatcherPriority.Background);
            };
        }

        public void Update(ITerminal terminal)
        {
            var quote = terminal.GetQuote(Symbol);

            if (quote == null || quote.Bid == 0 || quote.Ask == 0)
                return;

            Update((double)quote.Bid, (double)quote.Ask, quote.TimeStamp.ToMinutes());
        }

        public void Update(double bid, double ask, DateTime barTime)
        {
            if (Candles.Count > 0)
            {
                var lastCandle = Candles.Last();

                var dateTime = DateTimeAxis.ToDateTime(lastCandle.X);

                if (dateTime.Equals(barTime))
                {
                    lastCandle.Low = Math.Min(lastCandle.Low, bid);
                    lastCandle.High = Math.Max(lastCandle.High, bid);
                    lastCandle.Close = bid;
                }
                else
                {
                    Candles.Add(new HighLowItem(
                        x: DateTimeAxis.ToDouble(barTime),
                        high: bid,
                        low: bid,
                        open: lastCandle.Close,
                        close: bid));
                }
            }
            else
            {
                Candles.Add(new HighLowItem(
                    x: DateTimeAxis.ToDouble(barTime),
                    high: bid,
                    low: bid,
                    open: bid,
                    close: bid));
            }

            BidLineAnnotation.Text = bid.ToString(CultureInfo.InvariantCulture);
            BidLineAnnotation.Y = bid;

            AskLineAnnotation.Text = ask.ToString(CultureInfo.InvariantCulture);
            AskLineAnnotation.Y = ask;

            if (XAutoScroll)
            {
                var actualMax = XAxis.ActualMaximum;
                var newMaxX = DateTimeAxis.ToDouble(barTime) + PeriodToX(TimeSpan.FromMinutes(30));

                var panDelta = XAxis.Transform(-(newMaxX - actualMax) + XAxis.Offset);

                XAxis.Pan(panDelta);
                XAxis.AbsoluteMaximum = newMaxX;
            }

            Model.InvalidatePlot(true);
        }

        public static double PeriodToX(TimeSpan period)
        {
            var start = DateTime.Now;
            var end = start + period;

            return DateTimeAxis.ToDouble(end) - DateTimeAxis.ToDouble(start);
        }

        private void AdjustYExtent(HighLowSeries series, Axis xAxis, Axis yAxis)
        {
            //if (xAxis != null && yAxis != null && series.Items.Count != 0)
            //{
            //    var start = xAxis.ActualMinimum;
            //    var end = xAxis.ActualMaximum;

            //    var items = series.Items.FindAll(i => i.X >= start && i.X <= end);

            //    var min = double.MaxValue;
            //    var max = double.MinValue;

            //    for (var i = 0; i <= items.Count - 1; i++)
            //    {
            //        min = Math.Min(min, items[i].Low);
            //        max = Math.Max(max, items[i].High);
            //    }

            //    const double tolerance = 0.00000001;

            //    //if (Math.Abs(min - double.MaxValue) > tolerance)
            //    //    yAxis.AbsoluteMinimum = min - min * YMinMaxOffsetPercent;

            //    //if (Math.Abs(max - double.MinValue) > tolerance)
            //    //    yAxis.AbsoluteMaximum = max + max * YMinMaxOffsetPercent;

            //    //xAxis.

            //    //var extent = max - min;
            //    //var margin = max * 2; //extent * 0.5; //change the 0 by a value to add some extra up and down margin 

            //    //yAxis.Zoom(min - margin, max + margin);
            //}
        }
    }

    public class ExchangeViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        public IList<SymbolTabItem> Tabs { get; set; }

        public ExchangeViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ExchangeViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.AccountChanged += OnAccountChangedEventHandler;

            App.Terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnAccountChangedEventHandler(object sender, AccountChangedEventArgs args)
        {
            var account = args.NewAccount;

            if (account == null)
                return;

            Tabs = App.Account.Symbols
                .Select(s => new SymbolTabItem(s))
                .ToList();

            OnPropertyChanged(nameof(Tabs));
        }

        private void OnQuotesUpdatedEventHandler(object sender, MarketData.MarketDataEventArgs e)
        {
            if (!(sender is ITerminal terminal))
                return;

            Tabs.FirstOrDefault(s => s.Symbol.Name == e.Symbol.Name)
                ?.Update(terminal);
        }

        private void DesignerMode()
        {
            var dateTime = DateTime.Now.ToMinutes();

            Tabs = new List<SymbolTabItem>
            {
                new SymbolTabItem(DesignTime.Symbols.GetByName("LTC/BTC")),
                new SymbolTabItem(DesignTime.Symbols.GetByName("ETH/BTC")),
                new SymbolTabItem(DesignTime.Symbols.GetByName("XTZ/BTC")),
                new SymbolTabItem(DesignTime.Symbols.GetByName("XTZ/ETH"))
            };

            var ltcBtc = Tabs.First();


            ltcBtc.Update(0.0096716, 0.0096726, dateTime.AddMinutes(-7));
            ltcBtc.Update(0.009687, 0.009688, dateTime.AddMinutes(-7));

            ltcBtc.Update(0.009703, 0.009704, dateTime.AddMinutes(-6));

            ltcBtc.Update(0.0096906, 0.0096916, dateTime.AddMinutes(-5));

            ltcBtc.Update(0.0097035, 0.0097045, dateTime.AddMinutes(-4));
            ltcBtc.Update(0.009687, 0.009688, dateTime.AddMinutes(-4));

            ltcBtc.Update(0.0096712, 0.0096722, dateTime.AddMinutes(-3));
            ltcBtc.Update(0.0096606, 0.0096616, dateTime.AddMinutes(-3));
            ltcBtc.Update(0.009669, 0.009670, dateTime.AddMinutes(-3));

            ltcBtc.Update(0.0096559, 0.0096569, dateTime.AddMinutes(-2));
            ltcBtc.Update(0.0096395, 0.0096405, dateTime.AddMinutes(-2));
            ltcBtc.Update(0.00964, 0.009641, dateTime.AddMinutes(-2));

            ltcBtc.Update(0.0096406, 0.0096416, dateTime.AddMinutes(-1));
            ltcBtc.Update(0.0096416, 0.0096426, dateTime.AddMinutes(-1));
            ltcBtc.Update(0.0096402, 0.0096412, dateTime.AddMinutes(-1));

            ltcBtc.Update(0.0096402, 0.0096412, dateTime);
            ltcBtc.Update(0.0096207, 0.0096217, dateTime);
        }
    }
}