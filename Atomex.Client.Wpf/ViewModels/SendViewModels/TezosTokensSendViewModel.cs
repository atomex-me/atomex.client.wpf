﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.WalletViewModels;
using Atomex.Client.Wpf.Properties;
using Atomex.Core;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Tezos;
using Atomex.Wallet.Abstract;
using Atomex.TezosTokens;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel 
    {
        public const string DefaultCurrencyFormat = "F8";
        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";
        public const int MaxCurrencyDecimals = 9;

        private readonly IAtomexApp _app;
        private readonly IDialogViewer _dialogViewer;

        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public TezosTokenViewModel Token { get; set; }

        private ObservableCollection<WalletAddressViewModel> _fromAddresses;
        public ObservableCollection<WalletAddressViewModel> FromAddresses
        {
            get => _fromAddresses;
            set
            {
                _fromAddresses = value;
                OnPropertyChanged(nameof(FromAddresses));
            }
        }

        private string _from;
        public string From
        {
            get => _from;
            set
            {
                _from = value;
                OnPropertyChanged(nameof(From));

                Warning = string.Empty;
                Amount = _amount;
                Fee = _fee;

                UpdateCurrencyCode();
            }
        }

        public string FromBalance { get; set; }

        private string _tokenContract;
        public string TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));

                Warning = string.Empty;
                Amount = _amount;
                Fee = _fee;

                UpdateFromAddressList(_from, _tokenContract, _tokenId);
                UpdateCurrencyCode();
            }
        }

        private decimal _tokenId;
        public decimal TokenId
        {
            get => _tokenId;
            set
            {
                _tokenId = value;
                OnPropertyChanged(nameof(TokenId));

                Warning = string.Empty;
                Amount = _amount;
                Fee = _fee;

                UpdateFromAddressList(_from, _tokenContract, _tokenId);
                UpdateCurrencyCode();
            }
        }

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
                    Fee = _fee;
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

            CurrencyCode     = "";
            FeeCurrencyCode  = TezosConfig.Xtz;
            BaseCurrencyCode = DefaultBaseCurrencyCode;

            CurrencyFormat     = DefaultCurrencyFormat;
            FeeCurrencyFormat  = tezosConfig.FeeFormat;
            BaseCurrencyFormat = DefaultBaseCurrencyFormat;

            _tokenContract = tokenContract;
            _tokenId       = tokenId;

            UpdateFromAddressList(from, tokenContract, tokenId);
            UpdateCurrencyCode();

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

        protected async virtual void OnNextCommand()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (string.IsNullOrEmpty(To))
            {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!tezosConfig.IsValidAddress(To))
            {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (Fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            if (TokenContract == null || From == null)
            {
                Warning = "Invalid 'From' address or token contract address!";
                return;
            }

            if (!tezosConfig.IsValidAddress(TokenContract))
            {
                Warning = "Invalid token contract address!";
                return;
            }

            var fromTokenAddress = await GetTokenAddressAsync(_app.Account, From, TokenContract, TokenId);

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

            var xtzAddress = await _app.Account
                .GetAddressAsync(TezosConfig.Xtz, From);

            if (xtzAddress == null)
            {
                Warning = $"Insufficient funds for fee. Please update your balance for address {From}!";
                return;
            }

            if (xtzAddress.AvailableBalance() < _fee)
            {
                Warning = $"Insufficient funds for fee!";
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel(_dialogViewer, Dialogs.Send)
            {
                Currency           = tezosConfig,
                From               = From,
                To                 = To,
                Amount             = Amount,
                AmountInBase       = AmountInBase,
                BaseCurrencyCode   = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee                = Fee,
                UseDeafultFee      = UseDefaultFee,
                FeeInBase          = FeeInBase,
                CurrencyCode       = CurrencyCode,
                CurrencyFormat     = CurrencyFormat,

                FeeCurrencyCode    = FeeCurrencyCode,
                FeeCurrencyFormat  = FeeCurrencyFormat,

                TokenContract      = TokenContract,
                TokenId            = TokenId
            };

            _dialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
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

                if (TokenContract == null || From == null)
                {
                    Warning = "Invalid 'From' address or token contract address!";
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(_app.Account, From, TokenContract, TokenId);

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
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                {
                    Warning = "Invalid 'From' address or token contract address!";
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                if (UseDefaultFee)
                {
                    var fromTokenAddress = await GetTokenAddressAsync(_app.Account, From, TokenContract, TokenId);

                    if (fromTokenAddress == null)
                    {
                        Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                        return;
                    }

                    var tokenAccount = _app.Account
                        .GetTezosTokenAccount<TezosTokenAccount>(fromTokenAddress.Currency, TokenContract, TokenId);

                    var (estimatedFee, isEnougth) = await tokenAccount
                        .EstimateTransferFeeAsync(From);

                    if (!isEnougth)
                    {
                        Warning = $"Insufficient funds for fee. Minimum {estimatedFee} XTZ is required!";
                        return;
                    }

                    _fee = estimatedFee;
                }
                else
                {
                    var xtzAddress = await _app.Account
                        .GetAddressAsync(TezosConfig.Xtz, From);

                    if (xtzAddress == null)
                    {
                        Warning = $"Insufficient funds for fee. Please update your balance for address {From}!";
                        return;
                    }

                    _fee = Math.Min(fee, tezosConfig.GetMaximumFee());

                    if (xtzAddress.AvailableBalance() < _fee)
                    {
                        Warning = $"Insufficient funds for fee!";
                        return;
                    }
                }

                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
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

                var fromTokenAddress = await GetTokenAddressAsync(_app.Account, From, TokenContract, TokenId);

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

            AmountInBase = !string.IsNullOrEmpty(CurrencyCode)
                ? Amount * (quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode)?.Bid ?? 0m)
                : 0;

            FeeInBase = !string.IsNullOrEmpty(FeeCurrencyCode)
                ? Fee * (quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode)?.Bid ?? 0m)
                : 0;
        }

        public static async Task<WalletAddress> GetTokenAddressAsync(
            IAccount account,
            string address,
            string tokenContract,
            decimal tokenId)
        {
            var tezosAccount = account
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

        private void UpdateFromAddressList(string from, string tokenContract, decimal tokenId)
        {
            _fromAddresses = new ObservableCollection<WalletAddressViewModel>(GetFromAddressList(tokenContract, tokenId));

            var tempFrom = from;

            if (tempFrom == null)
            {
                var unspentAddresses = _fromAddresses.Where(w => w.AvailableBalance > 0);
                var unspentTokenAddresses = _fromAddresses.Where(w => w.TokenBalance > 0);

                tempFrom = unspentTokenAddresses.MaxByOrDefault(w => w.TokenBalance)?.Address ??
                    unspentAddresses.MaxByOrDefault(w => w.AvailableBalance)?.Address;
            }

            OnPropertyChanged(nameof(FromAddresses));

            From = tempFrom;
        }

        private async void UpdateCurrencyCode()
        {
            if (TokenContract == null || From == null)
                return;

            var tokenAddress = await GetTokenAddressAsync(_app.Account, From, TokenContract, TokenId);

            if (tokenAddress?.TokenBalance?.Symbol != null)
            {
                CurrencyCode = tokenAddress.TokenBalance.Symbol;
                CurrencyFormat = $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxCurrencyDecimals)}";
                OnPropertyChanged(nameof(AmountString));
            }
            else
            {
                CurrencyCode = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name ?? "TOKENS";
                CurrencyFormat = DefaultCurrencyFormat;
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private IEnumerable<WalletAddressViewModel> GetFromAddressList(string tokenContract, decimal tokenId)
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tezosAddresses = tezosAccount
                .GetAddressesAsync()
                .WaitForResult();

            var tokenAddresses = tokenContract != null
                ? tezosAccount.DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract)
                    .WaitForResult()
                : new List<WalletAddress>();

            return tezosAddresses
                .Concat(tokenAddresses)
                .GroupBy(w => w.Address)
                .Select(g =>
                {
                    // main address
                    var address = g.FirstOrDefault(w => w.Currency == tezosConfig.Name);

                    var tokenAddress = g.FirstOrDefault(w => w.Currency != tezosConfig.Name && w.TokenBalance?.TokenId == TokenId);

                    var tokenBalance = tokenAddress?.Balance ?? 0m;

                    var showTokenBalance = tokenBalance != 0;

                    var tokenCode = tokenAddress?.TokenBalance?.Symbol ?? "TOKENS";

                    return new WalletAddressViewModel
                    {
                        Address          = g.Key,
                        AvailableBalance = address?.AvailableBalance() ?? 0m,
                        CurrencyFormat   = tezosConfig.Format,
                        CurrencyCode     = tezosConfig.Name,
                        IsFreeAddress    = false,
                        ShowTokenBalance = showTokenBalance,
                        TokenBalance     = tokenBalance,
                        TokenFormat      = "F8",
                        TokenCode        = tokenCode
                    };
                })
                .ToList();
        }

        private void DesignerMode()
        {

        }
    }
}