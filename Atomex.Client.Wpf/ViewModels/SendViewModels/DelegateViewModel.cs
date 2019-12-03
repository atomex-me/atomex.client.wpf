using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Common;
using Atomex.Core;
using Atomex.Core.Entities;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        
        private Tezos xTezos;
        private WalletAddress _walletAddress;
        private TezosTransaction Tx;

        public WalletAddress WalletAddress
        {
            get => _walletAddress;
            set 
            {
                _walletAddress = value;
                OnPropertyChanged(nameof(WalletAddress));
            }
        }

        private string FeePriceFormat { get; set; }

        private List<BakerViewModel> _fromBakersList;
        public List<BakerViewModel> FromBakersList
        {
            get => _fromBakersList;
            private set 
            {
                _fromBakersList = value;
                OnPropertyChanged(nameof(FromBakersList));
                BakerViewModel = FromBakersList.FirstOrDefault();
            }
        }

        private List<WalletAddress> _fromAddressList;
        public List<WalletAddress> FromAddressList
        {
            get => _fromAddressList;
            private set 
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));
                
                WalletAddress = FromAddressList.FirstOrDefault();
            }
        }

        private BakerViewModel _bakerViewModel;
        public BakerViewModel BakerViewModel
        {
            get => _bakerViewModel;
            set
            {
                _bakerViewModel = value;
                OnPropertyChanged(nameof(BakerViewModel));
                
                Address = _bakerViewModel?.Address;
            }
        }
        
        public string FeeString
        {
            get => Fee.ToString(xTezos.FeeFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;
                FeeCurrencyCode = xTezos.FeeCode;
                BaseCurrencyCode = "USD";
                BaseCurrencyFormat = "$0.00";
                

                Fee = fee.TruncateByFormat(xTezos.FeeFormat);
            }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = xTezos.GetFeeAmount(_fee, _feePrice);

                    if (feeAmount > _walletAddress.Balance)
                        feeAmount = _walletAddress.Balance;

                    _fee = xTezos.GetFeeFromFeeAmount(feeAmount, _feePrice);

                    OnPropertyChanged(nameof(FeeString));
                    Warning = string.Empty;
                }

//                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }
        
        private string _baseCurrencyFormat;
        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }
        
        private decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }
        
        private string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }
        
        private string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }
        
        private decimal _feePrice;
        private decimal FeePrice
        {
            get => _feePrice;
            set
            {
                _feePrice = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = xTezos.GetFeeAmount(_fee, _feePrice);

                    if (feeAmount > _walletAddress.Balance)
                        feeAmount = _walletAddress.Balance;

                    _feePrice = xTezos.GetFeePriceFromFeeAmount(feeAmount, _fee);

                    OnPropertyChanged(nameof(FeePriceString));
                    Warning = string.Empty;
                }

//                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }
        
        public string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gasPrice))
                    return;

                FeePrice = gasPrice.TruncateByFormat(FeePriceFormat);
            }
        }
        
        private bool _useDefaultFee;
        public bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));
            }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }


        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer?.HideDelegateDialog();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(() =>
        {
            if (string.IsNullOrEmpty(Address)) {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!xTezos.IsValidAddress(Address)) {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Fee < 0) {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }
/*

            if (xTezos.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount) {
                Warning = Resources.SvAvailableFundsError;
                return;
            }*/

/*            var confirmationViewModel = new SendConfirmationViewModel(DialogViewer)
            {
                Currency = Currency,
                To = To,
                Amount = Amount,
                AmountInBase = AmountInBase,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee = Fee,
                FeeInBase = FeeInBase,
                FeePrice = FeePrice,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat
            };*/

            var result = SendDelegation().WaitForResult();

            if (result.HasError)
                Warning = result.Error.Description;
            else
            {
                var confirmationViewModel = new DelegateConfirmationViewModel(DialogViewer)
                {
                    Currency = xTezos,
                    WalletAddress = WalletAddress,
                    UseDefaultFee = UseDefaultFee,
                    Tx = Tx,
                    From = WalletAddress.Address,
                    To = Address,
                    BaseCurrencyCode = BaseCurrencyCode,
                    BaseCurrencyFormat = BaseCurrencyFormat,
                    Fee = Fee,
                    FeeInBase = FeeInBase,
                    FeePrice = FeePrice,
                    CurrencyCode = xTezos.FeeCode,
                    CurrencyFormat = xTezos.FeeFormat
                };
                
                Navigation.Navigate(
                    uri: Navigation.DelegateConfirmationAlias,
                    context: confirmationViewModel);
            }
        }));
        
        public DelegateViewModel()
        {
//#if DEBUG
            //if (Env.IsInDesignerMode())
            DesignerMode();
//#endif
        }

        public DelegateViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            
            xTezos = App.Account.Currencies.Get<Tezos>();
            UseDefaultFee = true;
            SubscribeToServices();
            LoadBakerList().FireAndForget();
            PrepareWallet().WaitForResult();
        }

        private async Task LoadBakerList()
        {
            List<BakerViewModel> bakers = null;

            try
            {
                await Task.Run(async () =>
                {
                    var bbApi = new BbApi(xTezos);
                    bakers = (await bbApi.GetBakers(App.Account.Network)
                        .ConfigureAwait(false)).Select(x => new BakerViewModel
                    {
                        Address = x.Address,
                        Logo = x.Logo,
                        Name = x.Name,
                        Fee = x.Fee
                    }).ToList();
                });
            }
            catch (Exception e)
            {
                Log.Error(e.Message, "Error while fetching bakers list");
            }

            if (Application.Current.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FromBakersList = bakers;
                }, DispatcherPriority.Background);
            }
        }

        private async Task PrepareWallet(CancellationToken cancellationToken = default)
        {
            FromAddressList = (await App.Account
                    .GetUnspentAddressesAsync(xTezos, cancellationToken).ConfigureAwait(false))
                .OrderByDescending(x => x.Balance)
                .ToList();

            if (!FromAddressList?.Any() ?? false)
            {
                Warning = "You don't have non-empty accounts";
                return;
            }

            WalletAddress = FromAddressList.FirstOrDefault();
        }

        private async Task<Result<string>> SendDelegation(CancellationToken cancellationToken = default)
        {
            if(_walletAddress == null)
                return new Result<string>(new Error(Errors.InvalidWallets, "You don't have non-empty accounts"));
            
            var wallet = (HdWallet) App.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var rpc = new Rpc(xTezos.RpcNodeUri);
            JObject delegateData;
            try
            {
                delegateData = await rpc.GetDelegate(_address)
                    .ConfigureAwait(false);
            }
            catch
            {
                return new Result<string>(new Error(Errors.WrongDelegationAddress, "Wrong delegation address"));
            }
            
            if (delegateData["deactivated"].Value<bool>())
                return new Result<string>(new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one"));

            var delegators = delegateData["delegated_contracts"]?.Values<string>();
            if (delegators.Contains(_walletAddress.Address))
            {
                return new Result<string>(new Error(Errors.AlreadyDelegated, $"Already delegated from {_walletAddress.Address} to {_address}"));
            }
            var tx = new TezosTransaction
            {
                StorageLimit = xTezos.StorageLimit,
                GasLimit = xTezos.GasLimit,
                From = _walletAddress.Address,
                To = _address,
                Fee = Fee * 1_000_000,
                Currency = xTezos
            };

            try
            {
                var calculatedFee = await tx.AutoFillAsync(keyStorage, _walletAddress, UseDefaultFee);
                if(!calculatedFee)
                    return new Result<string>(new Error(Errors.TransactionCreationError, $"Autofill transaction failed"));
                Fee = tx.Fee;
                Tx = tx;
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill delegation error");
                return new Result<string>(new Error(Errors.TransactionCreationError, $"Autofill delegation error. Try again later"));
            }
            
            return new Result<string>("Successful check");
        }
        
        public void Show()
        {
            Navigation.Navigate(
                uri: Navigation.DelegateAlias,
                context: this);
        }
        
        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            FeeInBase = xTezos.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }

        private void DesignerMode()
        {
            FromBakersList = new List<BakerViewModel>()
            {
                new BakerViewModel()
                {
                    Logo = "https://api.baking-bad.org/logos/tezoshodl.png", //"tezoshodl.png",
                    Name = "TezosHODL",
                    Address = "tz1sdfldjsflksjdlkf123sfa",
                    Fee = 5
                }
            };

            BakerViewModel = FromBakersList.FirstOrDefault();

            _address = "tz1sdfldjsflksjdlkf123sfa";
            _fee = 5;
            _feeInBase = 123m;
        }
    }
}