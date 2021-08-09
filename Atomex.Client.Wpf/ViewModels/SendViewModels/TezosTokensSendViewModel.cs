using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Helpers;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.WalletViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel 
    {
        private readonly IAtomexApp _app;
        private readonly IDialogViewer _dialogViewer;

        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public TezosTokenViewModel Token { get; set; }
        public ObservableCollection<string> FromAddresses { get; set; }
        public string From { get; set; }
        public string FromBalance { get; set; }
        public string TokenContract { get; set; }
        public decimal TokenId { get; set; }

        protected string _to;
        public virtual string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));

                Warning = string.Empty;
            }
        }

        public string CurrencyFormat { get; set; }
        public string FeeCurrencyFormat { get; set; }

        private string _baseCurrencyFormat;
        public virtual string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        protected decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
        }

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set { _isAmountUpdating = value; OnPropertyChanged(nameof(IsAmountUpdating)); }
        }

        public string AmountString
        {
            get => Amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    return;

                Amount = amount.TruncateByFormat(CurrencyFormat);
            }
        }

        private bool _isFeeUpdating;
        public bool IsFeeUpdating
        {
            get => _isFeeUpdating;
            set { _isFeeUpdating = value; OnPropertyChanged(nameof(IsFeeUpdating)); }
        }

        protected decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { UpdateFee(value); }
        }

        public virtual string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(FeeCurrencyFormat);
            }
        }

        protected bool _useDefaultFee;
        public virtual bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    Amount = _amount; // recalculate amount and fee using default fee
            }
        }

        protected decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        protected decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        protected string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        protected string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        protected string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        public TezosTokensSendViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokensSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            string from = null,
            string tokenContract = null,
            decimal tokenId = 0)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tezosAddresses = tezosAccount
                .GetAddressesAsync()
                .WaitForResult()
                .Select(w => w.Address);

            CurrencyCode     = null;
            FeeCurrencyCode  = TezosConfig.Xtz;
            BaseCurrencyCode = "USD";

            CurrencyFormat     = "F8";
            FeeCurrencyFormat  = tezosConfig.FeeFormat;
            BaseCurrencyFormat = "$0.00";

            FromAddresses = new ObservableCollection<string>(tezosAddresses);
            From          = from;
            TokenContract = tokenContract;
            TokenId       = tokenId;

            SubscribeToServices();

            UseDefaultFee = true;
        }

        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            _dialogViewer.HideDialog(Dialogs.Send);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextCommand);

        protected ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= new Command(OnMaxClick);

        protected virtual void OnNextCommand()
        {
            //if (string.IsNullOrEmpty(To))
            //{
            //    Warning = Resources.SvEmptyAddressError;
            //    return;
            //}

            //if (!Currency.IsValidAddress(To))
            //{
            //    Warning = Resources.SvInvalidAddressError;
            //    return;
            //}

            //if (Amount <= 0)
            //{
            //    Warning = Resources.SvAmountLessThanZeroError;
            //    return;
            //}

            //if (Fee <= 0)
            //{
            //    Warning = Resources.SvCommissionLessThanZeroError;
            //    return;
            //}

            //var isToken = Currency.FeeCurrencyName != Currency.Name;

            //var feeAmount = !isToken ? Fee : 0;

            //if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            //{
            //    Warning = Resources.SvAvailableFundsError;
            //    return;
            //}

            //var confirmationViewModel = new SendConfirmationViewModel(DialogViewer, Dialogs.Send)
            //{
            //    Currency = Currency,
            //    To = To,
            //    Amount = Amount,
            //    AmountInBase = AmountInBase,
            //    BaseCurrencyCode = BaseCurrencyCode,
            //    BaseCurrencyFormat = BaseCurrencyFormat,
            //    Fee = Fee,
            //    UseDeafultFee = UseDefaultFee,
            //    FeeInBase = FeeInBase,
            //    CurrencyCode = CurrencyCode,
            //    CurrencyFormat = CurrencyFormat,

            //    FeeCurrencyCode = FeeCurrencyCode,
            //    FeeCurrencyFormat = FeeCurrencyFormat
            //};

            //_dialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
        }

        protected virtual async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;
            Warning = string.Empty;

            _amount = amount;
            OnPropertyChanged(nameof(AmountString));

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                    return;

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(From, TokenContract, TokenId);

                if (fromTokenAddress == null)
                {
                    Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                    return;
                }

                if (_amount > fromTokenAddress.Balance)
                {
                    Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                    return;
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual async void UpdateFee(decimal fee)
        {
            //if (IsFeeUpdating)
            //    return;

            //IsFeeUpdating = true;

            //_fee = Math.Min(fee, Currency.GetMaximumFee());

            //Warning = string.Empty;

            //try
            //{
            //    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

            //    if (_amount == 0)
            //    {
            //        if (Currency.GetFeeAmount(_fee, defaultFeePrice) > CurrencyViewModel.AvailableAmount)
            //            Warning = Resources.CvInsufficientFunds;

            //        return;
            //    }

            //    if (!UseDefaultFee)
            //    {
            //        var account = App.Account
            //            .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

            //        var estimatedFeeAmount = _amount != 0
            //            ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
            //            : 0;

            //        var (maxAmount, maxFeeAmount, _) = await account
            //            .EstimateMaxAmountToSendAsync(
            //                to: To,
            //                type: BlockchainTransactionType.Output,
            //                fee: 0,
            //                feePrice: 0,
            //                reserve: false);

            //        var availableAmount = Currency is BitcoinBasedConfig
            //            ? CurrencyViewModel.AvailableAmount
            //            : maxAmount + maxFeeAmount;

            //        var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

            //        if (_amount + feeAmount > availableAmount)
            //        {
            //            Warning = Resources.CvInsufficientFunds;
            //            IsAmountUpdating = false;
            //            return;
            //        }
            //        else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
            //        {
            //            Warning = Resources.CvLowFees;
            //        }

            //        OnPropertyChanged(nameof(FeeString));
            //    }

            //    OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            //}
            //finally
            //{
            //    IsFeeUpdating = false;
            //}
        }

        protected virtual async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(From, TokenContract, TokenId);

                if (fromTokenAddress == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                    return;
                }

                _amount = fromTokenAddress.Balance;
                OnPropertyChanged(nameof(AmountString));

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            if (string.IsNullOrEmpty(CurrencyCode))
            {
                AmountInBase = 0;
                FeeInBase    = 0;
                return;
            }

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase    = Fee * (quote?.Bid ?? 0m);
        }

        private async Task<WalletAddress> GetTokenAddressAsync(
            string address,
            string tokenContract,
            decimal tokenId)
        {
            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var fa12Address = await tezosAccount
                .DataRepository
                .GetTezosTokenAddressAsync("FA12", tokenContract, tokenId, address);

            if (fa12Address != null)
                return fa12Address;

            var fa2Address = await tezosAccount
                .DataRepository
                .GetTezosTokenAddressAsync("FA2", tokenContract, tokenId, address);

            return fa2Address;
        }

        private void DesignerMode()
        {

        }
    }
}