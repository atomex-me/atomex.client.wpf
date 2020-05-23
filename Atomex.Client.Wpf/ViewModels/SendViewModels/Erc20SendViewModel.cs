using System;
using System.Linq;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
    {
        public override Currency Currency
        {
            get => _currency;
            set
            {
                if (_currency != null && _currency != value)
                {
                    DialogViewer.HideDialog(Dialogs.Send);

                    var sendViewModel = SendViewModelCreator.CreateViewModel(App, DialogViewer, value);
                    var sendPageId = SendViewModelCreator.GetSendPageId(value);

                    DialogViewer.ShowDialog(Dialogs.Send, sendViewModel, defaultPageId: sendPageId);
                    return;
                }

                _currency = value;
                OnPropertyChanged(nameof(Currency));

                CurrencyViewModel = FromCurrencies.FirstOrDefault(c => c.Currency.Name == Currency.Name);

                _amount = 0;
                OnPropertyChanged(nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(nameof(FeeString));

                _feePrice = 0;
                OnPropertyChanged(nameof(FeePriceString));

                OnPropertyChanged(nameof(TotalFeeString));

                FeePriceFormat = _currency.FeePriceFormat;
                FeePriceCode = _currency.FeePriceCode;

                Warning = string.Empty;
            }
        }

        public override string TotalFeeCurrencyCode => Currency.FeeCurrencyName;

        public override decimal FeePrice
        {
            get => _feePrice;
            set { UpdateFeePrice(value); }
        }


        public Erc20SendViewModel()
            : base()
        {
        }

        public Erc20SendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer, currency)
        {
        }

        protected virtual async void UpdateFeePrice(decimal value)
        {
            _feePrice = value;

            if (!UseDefaultFee)
            {
                var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                var ethAddress = (await App.Account
                    .GetUnspentAddressesAsync(Currency.FeeCurrencyName))
                    .ToList()
                    ?.MaxBy(w => w.AvailableBalance());

                if (ethAddress != null)
                {
                    feeAmount = Math.Min(ethAddress.AvailableBalance(), feeAmount);

                    _feePrice = Currency.GetFeePriceFromFeeAmount(feeAmount, _fee);
                }

                OnPropertyChanged(nameof(FeePriceString));
                Warning = string.Empty;

                OnPropertyChanged(nameof(TotalFeeString));
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            var previousAmount = _amount;
            _amount = amount;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? (_amount < availableAmount
                            ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                            : null)
                        : 0;

                    if (estimatedFeeAmount == null)
                    {
                        if (maxAmount > 0)
                        {
                            _amount = maxAmount;
                            estimatedFeeAmount = maxFeeAmount;
                        }
                        else
                        {
                            _amount = previousAmount;
                            Warning = Resources.CvInsufficientFunds;

                            IsAmountUpdating = false;
                            return;
                        }
                    }

                    if (_amount > availableAmount)
                        _amount = Math.Max(availableAmount, 0);

                    if (_amount == 0)
                        estimatedFeeAmount = 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = Currency.GetDefaultFeePrice();
                    OnPropertyChanged(nameof(FeePriceString));

                    OnPropertyChanged(nameof(TotalFeeString));

                    Warning = string.Empty;
                }
                else
                {
                    var (maxAmount, _, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, false);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount;

                    if (_amount > availableAmount)
                        _amount = Math.Max(availableAmount, 0);

                    OnPropertyChanged(nameof(AmountString));

                    if (_fee != 0)
                        Fee = _fee;

                    OnPropertyChanged(nameof(TotalFeeString));

                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override async void UpdateFee(decimal fee)
        {
            if (_amount == 0)
            {
                _fee = 0;
                return;
            }

            try
            {
                IsFeeUpdating = true;

                _fee = Math.Min(fee, Currency.GetMaximumFee());

                if (!UseDefaultFee)
                {
                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                    if (feeAmount < estimatedFeeAmount.Value)
                        _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());

                    if (_amount == 0)
                        _fee = 0;

                    OnPropertyChanged(nameof(AmountString));
                    OnPropertyChanged(nameof(GasString));
                    OnPropertyChanged(nameof(TotalFeeString));

                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (ethQuote?.Bid ?? 0m);
        }
    }
}

//using System;
//using System.Linq;
//using Atomex.Blockchain.Abstract;
//using Atomex.Client.Wpf.Common;
//using Atomex.Client.Wpf.Controls;
//using Atomex.Client.Wpf.Properties;
//using Atomex.MarketData.Abstract;
//using Atomex.Client.Wpf.ViewModels.Abstract;
//using Atomex.Common;
//using Atomex.Core;
//using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;

//namespace Atomex.Client.Wpf.ViewModels.SendViewModels
//{
//    public class Erc20SendViewModel : SendViewModel
//    {
//        private Currency _chainCurrency { get; set; }

//        public override CurrencyViewModel CurrencyViewModel
//        {
//            get => _currencyViewModel;
//            set
//            {
//                _currencyViewModel = value;

//                CurrencyCode = _currencyViewModel?.CurrencyCode;
//                ChainCurrencyCode = _currencyViewModel?.ChainCurrencyCode;
//                FeeCurrencyCode = _currencyViewModel?.FeeCurrencyCode;
//                BaseCurrencyCode = _currencyViewModel?.BaseCurrencyCode;

//                CurrencyFormat = _currencyViewModel?.CurrencyFormat;
//                FeeCurrencyFormat = CurrencyViewModel?.FeeCurrencyFormat;
//                BaseCurrencyFormat = _currencyViewModel?.BaseCurrencyFormat;
//            }
//        }

//        public override decimal Amount
//        {
//            get => _amount;
//            set
//            {
//                var previousAmount = _amount;
//                _amount = value;

//                if (UseDefaultFee)
//                {
//                    var estimatedFee = _amount != 0
//                        ? (_amount < CurrencyViewModel.AvailableAmount
//                            ? App.Account
//                                .EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
//                                .WaitForResult()
//                            : null)
//                        : 0;

//                    if (estimatedFee == null)
//                    {
//                        var (maxAmount, maxFee, _) = App.Account
//                            .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output)
//                            .WaitForResult();

//                        if (maxAmount > 0)
//                        {
//                            _amount = maxAmount;
//                            estimatedFee = maxFee;
//                        }
//                        else
//                        {
//                            _amount = previousAmount;
//                            Warning = Resources.CvInsufficientFunds;
//                            return;
//                        }
//                    }
//                    //todo check if it is possible
//                    if (_amount > CurrencyViewModel.AvailableAmount)
//                        _amount = Math.Max(CurrencyViewModel.AvailableAmount, 0);

//                    if (_amount == 0)
//                        estimatedFee = 0;

//                    OnPropertyChanged(nameof(AmountString));

//                    _fee = Currency.GetFeeFromFeeAmount(estimatedFee.Value, Currency.GetDefaultFeePrice());
//                    OnPropertyChanged(nameof(FeeString));

//                    if (UseFeePrice)
//                    {
//                        _feePrice = Currency.GetDefaultFeePrice();
//                        OnPropertyChanged(nameof(FeePriceString));
//                    }

//                    Warning = string.Empty;
//                }
//                else
//                {
//                    var (maxAmount, maxFee, _) = App.Account
//                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output)
//                        .WaitForResult();

//                    if (_amount > maxAmount)
//                    {
//                        if (maxAmount > 0)
//                        {
//                            _amount = maxAmount;
//                        }
//                        else
//                        {
//                            _amount = previousAmount;
//                            Warning = Resources.CvInsufficientFunds;
//                            return;
//                        }
//                    }

//                    if (_amount > CurrencyViewModel.AvailableAmount)
//                        _amount = Math.Max(CurrencyViewModel.AvailableAmount, 0);

//                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

//                    if (feeAmount > maxFee)
//                        FeePrice = _feePrice;

//                    OnPropertyChanged(nameof(AmountString));

//                    Warning = string.Empty;
//                }

//                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
//            }
//        }

//        public override decimal Fee
//        {
//            get => _fee;
//            set
//            {
//                _fee = value;

//                if (!UseDefaultFee)
//                {
//                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

//                    var maxFeeAmount = App.Account
//                        .EstimateMaxFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
//                        .WaitForResult();

//                    feeAmount = Math.Min(feeAmount, maxFeeAmount);

//                    _fee = Currency.GetFeeFromFeeAmount(feeAmount, _feePrice);

//                    OnPropertyChanged(nameof(FeeString));
//                    Warning = string.Empty;
//                }

//                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
//            }
//        }

//        public override decimal FeePrice
//        {
//            get => _feePrice;
//            set
//            {
//                _feePrice = value;

//                if (!UseDefaultFee)
//                {
//                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

//                    var maxFeeAmount = App.Account
//                        .EstimateMaxFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
//                        .WaitForResult();

//                    feeAmount = Math.Min(feeAmount, maxFeeAmount);

//                    _feePrice = Currency.GetFeePriceFromFeeAmount(feeAmount, _fee);

//                    OnPropertyChanged(nameof(FeePriceString));
//                    Warning = string.Empty;
//                }

//                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
//            }
//        }

//        private string _chainCurrencyCode;
//        public string ChainCurrencyCode
//        {
//            get => _chainCurrencyCode;
//            set { _chainCurrencyCode = value; OnPropertyChanged(nameof(ChainCurrencyCode)); }
//        }

//        public Erc20SendViewModel()
//        {
//#if DEBUG
//            if (Env.IsInDesignerMode())
//                DesignerMode();
//#endif
//        }

//        public Erc20SendViewModel(
//            IAtomexApp app,
//            IDialogViewer dialogViewer,
//            Currency currency,
//            Currency chainCurrency)
//        {
//            App = app ?? throw new ArgumentNullException(nameof(app));
//            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

//            FromCurrencies = App.Account.Currencies
//                .Where(c => c.IsTransactionsAvailable)
//                .Select(CurrencyViewModelCreator.CreateViewModel)
//                .ToList();

//            Currency = currency;
//            _chainCurrency = chainCurrency;

//            SubscribeToServices();
//        }
//        private void SubscribeToServices()
//        {
//            if (App.HasQuotesProvider)
//                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
//        }

//        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
//        {
//            if (!(sender is ICurrencyQuotesProvider quotesProvider))
//                return;

//            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

//            AmountInBase = Amount * (quote?.Bid ?? 0m);
//            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
//        }

//        private void DesignerMode()
//        {
//            FromCurrencies = DesignTime.Currencies
//                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
//                .ToList();

//            Currency = FromCurrencies[0].Currency;
//            _to = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
//            _amount = 0.00001234m;
//            _amountInBase = 10.23m;
//            _fee = 0.0001m;
//            _feeInBase = 8.43m;
//        }
//    }
//}