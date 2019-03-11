using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Atomix.Core.Entities;
using Atomix.Client.Wpf.Common;
using Atomix.Common;
using Atomix.Subsystems.Abstract;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using DateTimeAxis = OxyPlot.Axes.DateTimeAxis;
using LinearAxis = OxyPlot.Axes.LinearAxis;

namespace Atomix.Client.Wpf.ViewModels
{
    public class SymbolTabItem : BaseViewModel
    {
        public Symbol Symbol { get; }
        public string Header => Symbol.Name;
        public CandleStickSeries CandleStickSeries;
        public LineAnnotation BidLineAnnotation;
        public PlotModel Model { get; set; }
        public DateTimeAxis XAxis { get; set; }
        public string XAxisFormat { get; } = "dd MMM HH:mm";
        public bool XAutoScroll { get; } = true;
        public double YMinMaxOffsetPercent { get; } = 0.01;

        public OxyColor PlotAreaBorderColor { get; } = OxyColors.Gray;
        public OxyColor TextColor { get; } = OxyColors.WhiteSmoke;
        public OxyColor TickLineColor { get; } = OxyColors.WhiteSmoke;
        public OxyColor AxisLineColor { get; } = OxyColors.WhiteSmoke;
        public OxyColor MajorGridLineColor { get; } = OxyColor.FromRgb(r: 31, g: 52, b: 86);
        public OxyColor IncreasingColor { get; } = OxyColor.FromRgb(r: 50, g: 205, b: 50);
        public OxyColor DecreasingColor { get; } = OxyColor.FromRgb(r: 255, g: 99, b: 71);
        public OxyColor BidLineColor { get; } = OxyColors.Red;

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
                
                //MinimumRange = PeriodToX(TimeSpan.FromHours(1))
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
                Text = (0.0m).ToString(CultureInfo.InvariantCulture),
                //TextLinePosition = 1, 
                TextColor = DecreasingColor,
                X = 0,
                Y = 0
            };

            Model.Annotations.Add(BidLineAnnotation);
        }

        public void Update(ITerminal terminal)
        {
            var quote = terminal.GetQuote(Symbol);

            if (quote == null || quote.Bid == 0)
                return;

            var bid = (double) quote.Bid;
            var barTime = quote.TimeStamp.ToMinutes();

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

        private double PeriodToX(TimeSpan period)
        {
            var start = DateTime.Now;
            var end = start + period;

            return DateTimeAxis.ToDouble(end) - DateTimeAxis.ToDouble(start);
        }

        private void AdjustYExtent(HighLowSeries series, Axis xAxis, Axis yAxis)
        {
            if (xAxis != null && yAxis != null && series.Items.Count != 0)
            {
                var start = xAxis.ActualMinimum;
                var end = xAxis.ActualMaximum;

                var items = series.Items.FindAll(i => i.X >= start && i.X <= end);

                var min = double.MaxValue;
                var max = double.MinValue;

                for (var i = 0; i <= items.Count - 1; i++)
                {
                    min = Math.Min(min, items[i].Low);
                    max = Math.Max(max, items[i].High);
                }

                const double tolerance = 0.00000001;

                if (Math.Abs(min - double.MaxValue) > tolerance)
                    yAxis.AbsoluteMinimum = min - min * YMinMaxOffsetPercent;

                if (Math.Abs(max - double.MinValue) > tolerance)
                    yAxis.AbsoluteMaximum = max + max * YMinMaxOffsetPercent;

                //xAxis.

                //var extent = max - min;
                //var margin = extent * 1; //change the 0 by a value to add some extra up and down margin 

                //yAxis.Zoom(min - margin, max + margin);
            }
        }
    }

    public class ExchangeViewModel : BaseViewModel
    {
        public IList<SymbolTabItem> Tabs { get; set; }

        public ExchangeViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode()) {
                DesignerMode();
                return;
            }
#endif

            Tabs = Symbols.Available
                .Select(s => new SymbolTabItem(s))
                .ToList();

            SubscribeToServices(App.AtomixApp);
        }

        private void SubscribeToServices(AtomixApp app)
        {
            app.Terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, MarketData.MarketDataEventArgs e)
        {
            if (!(sender is ITerminal terminal))
                return;

            Tabs.FirstOrDefault(s => s.Symbol.Name.Equals(e.Symbol.Name))
                ?.Update(terminal);
        }

        private void DesignerMode()
        {
            Tabs = new List<SymbolTabItem>
            {
                new SymbolTabItem(Symbols.LtcBtc),
                new SymbolTabItem(Symbols.LtcBtc),
                new SymbolTabItem(Symbols.LtcBtc)
            };
        }
    }
}