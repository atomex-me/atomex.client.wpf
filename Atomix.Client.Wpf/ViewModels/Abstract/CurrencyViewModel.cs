using System;
using System.Threading.Tasks;
using System.Windows.Media;
using Atomix.Core.Entities;
using Atomix.MarketData.Abstract;
using Atomix.Wallet;
using Atomix.Wallet.Abstract;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels.Abstract
{
    public abstract class CurrencyViewModel : BaseViewModel, IDisposable
    {
        public const string PathToImages = "pack://application:,,,/Atomix.Client.Wpf;component/Resources/Images";

        protected IAccount Account { get; set; }
        protected ICurrencyQuotesProvider QuotesProvider { get; set; }

        public event EventHandler AmountUpdated;

        public Currency Currency { get; set; }
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
        public decimal LockedAmount { get; set; }
        public decimal LockedAmountInBase { get; set; }
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

        public abstract Task UpdateAsync();

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
                if (Currency.Name.Equals(args.Currency.Name))
                    await UpdateAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, $"error for currency {args.Currency.Name}");
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        protected void UpdateQuotesInBaseCurrency(ICurrencyQuotesProvider quotesProvider)
        {
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(TotalAmountInBase));

            AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(AvailableAmountInBase));

            UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(UnconfirmedAmountInBase));

            LockedAmountInBase = LockedAmount * (quote?.Bid ?? 0m);
            OnPropertyChanged(nameof(LockedAmountInBase));

            AmountUpdated?.Invoke(this, EventArgs.Empty);
        }

        public static string PathToImage(string imageName)
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