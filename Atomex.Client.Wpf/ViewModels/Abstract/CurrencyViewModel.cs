using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels.Abstract
{
    public abstract class CurrencyViewModel : BaseViewModel, IDisposable
    {
        private const string PathToImages = "pack://application:,,,/Atomex.Client.Wpf;component/Resources/Images";

        private IAccount Account { get; set; }
        private ICurrencyQuotesProvider QuotesProvider { get; set; }

        public event EventHandler AmountUpdated;

        public Currency Currency { get; set; }
        public Currency ChainCurrency { get; set; }
        public string Header { get; set; }
        public Brush IconBrush { get; set; }
        public Brush UnselectedIconBrush { get; set; }
        public Brush IconMaskBrush { get; set; }
        public Color AccentColor { get; set; }
        public Color AmountColor { get; set; } 
        public string IconPath { get; set; }
        public string LargeIconPath { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TotalAmountInBase { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal AvailableAmountInBase { get; set; }
        public decimal UnconfirmedAmount { get; set; }
        public decimal UnconfirmedAmountInBase { get; set; }
        //public decimal LockedAmount { get; set; }
        //public decimal LockedAmountInBase { get; set; }
        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string BaseCurrencyCode => "USD"; // todo: use base currency from settings
        public string CurrencyFormat => Currency.Format;
        public string FeeCurrencyFormat => Currency.FeeFormat;
        public string BaseCurrencyFormat => "$0.00"; // todo: use base currency format from settings
        public string FeeName { get; set; }

        public bool HasUnconfirmedAmount => UnconfirmedAmount != 0;

        private decimal _portfolioPercent;
        public decimal PortfolioPercent
        {
            get => _portfolioPercent;
            set { _portfolioPercent = value; OnPropertyChanged(nameof(PortfolioPercent)); }
        }

        protected CurrencyViewModel(Currency currency)
        {
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        protected virtual async Task UpdateAsync()
        {
            var balance = await Account
                .GetBalanceAsync(Currency.Name)
                .ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TotalAmount = balance.Confirmed;
                OnPropertyChanged(nameof(TotalAmount));

                AvailableAmount = balance.Available;
                OnPropertyChanged(nameof(AvailableAmount));

                UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome;
                OnPropertyChanged(nameof(UnconfirmedAmount));
                OnPropertyChanged(nameof(HasUnconfirmedAmount));

                UpdateQuotesInBaseCurrency(QuotesProvider);

            }, DispatcherPriority.Background);
        }

        public Task UpdateInBackgroundAsync()
        {
            return Task.Run(UpdateAsync);
        }

        public void SubscribeToUpdates(IAccount account)
        {
            Account = account;
            Account.BalanceUpdated += OnBalanceChangedEventHandler;
        }

        public void SubscribeToRatesProvider(ICurrencyQuotesProvider quotesProvider)
        {
            QuotesProvider = quotesProvider;
            QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async void OnBalanceChangedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name.Equals(args.Currency))
                    await UpdateAsync()
                        .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for currency {args.Currency}");
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        private void UpdateQuotesInBaseCurrency(ICurrencyQuotesProvider quotesProvider)
        {
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(TotalAmountInBase));

            AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(AvailableAmountInBase));

            UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(UnconfirmedAmountInBase));

            //LockedAmountInBase = LockedAmount * (quote?.Bid ?? 0m);
            //OnPropertyChanged(nameof(LockedAmountInBase));

            AmountUpdated?.Invoke(this, EventArgs.Empty);
        }

        protected static string PathToImage(string imageName)
        {
            return $"{PathToImages}/{imageName}";
        }

        #region IDisposable Support
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Account != null)
                        Account.BalanceUpdated -= OnBalanceChangedEventHandler;

                    if (QuotesProvider != null)
                        QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
                }

                Account = null;
                Currency = null;

                _disposedValue = true;
            }
        }

        ~CurrencyViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}