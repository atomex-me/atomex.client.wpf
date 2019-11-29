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
using Atomex.Wallet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        
        private Tezos xTezos;
        private WalletAddress WalletAddress;
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

                    if (feeAmount > WalletAddress.Balance)
                        feeAmount = WalletAddress.Balance;

                    _fee = xTezos.GetFeeFromFeeAmount(feeAmount, _feePrice);

                    OnPropertyChanged(nameof(FeeString));
                    Warning = string.Empty;
                }

//                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
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

                    if (feeAmount > WalletAddress.Balance)
                        feeAmount = WalletAddress.Balance;

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
                Navigation.Navigate(
                uri: Navigation.MessageAlias,
                context: MessageViewModel.Success(
                    text: $"Successful delegation! \nOperation hash: {result.Value}",
                    nextAction: () => {DialogViewer?.HideDelegateDialog();}));
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
                    bakers = (await GetBakers()
                            .ConfigureAwait(false))
                        .ToList();
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
            var walletAddresses = await App.Account
                .GetUnspentAddressesAsync(xTezos, cancellationToken);

            if (!walletAddresses?.Any() ?? false)
            {
                Warning = "You don't have non-empty accounts";
                return;
            }

            WalletAddress = walletAddresses.MaxBy(x => x.Balance);
        }

        private async Task<Result<string>> SendDelegation(CancellationToken cancellationToken = default)
        {
            if(WalletAddress == null)
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
            if (delegators.Contains(WalletAddress.Address))
            {
                return new Result<string>(new Error(Errors.AlreadyDelegated, $"Already delegated from {WalletAddress.Address} to {_address}"));
            }

            var tx = new TezosTransaction
            {
                StorageLimit = xTezos.StorageLimit,
                GasLimit = xTezos.GasLimit,
                From = WalletAddress.Address,
                To = _address,
                Fee = Fee * 1_000_000,
                Currency = xTezos
            };

            try
            {
                var signResult = await tx
                    .SignDelegationOperationAsync(keyStorage, WalletAddress, cancellationToken, UseDefaultFee)
                    .ConfigureAwait(false);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");
                    return new Result<string>(new Error(Errors.TransactionSigningError, "Transaction signing error"));
                }

                var result = await xTezos.BlockchainApi
                    .BroadcastAsync(tx, cancellationToken)
                    .ConfigureAwait(false);

                return result.HasError
                    ? new Result<string>(new Error(Errors.TransactionBroadcastError, result.Error.Description))
                    : new Result<string>(result.Value);
            }
            catch
            {
                return new Result<string>(new Error(Errors.TransactionBroadcastError,
                    "Something went wrong. Try again later"));
            }
        }

        private async Task<IEnumerable<BakerViewModel>> GetBakers(CancellationToken cancellationToken = default)
        {
            var baseUri = "https://api.baking-bad.org/";

            if (App.Account.Network == Network.TestNet)
            {
                return new List<BakerViewModel>();
            }
            
            var rpc = new Rpc(xTezos.RpcNodeUri);

            var level = (await rpc.GetHeader())["level"].ToObject<int>();
            var currentCycle = (level - 1) / 4096;
            
            return await HttpHelper.GetAsync(
                    baseUri: baseUri,
                    requestUri: "v1/bakers?insurance=true&configs=true&rating=true",
                    responseHandler: response => ParseBakersToViewModel(JsonConvert.DeserializeObject<List<Baker>>(response.Content.ReadAsStringAsync().WaitForResult()), currentCycle),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        private IEnumerable<BakerViewModel> ParseBakersToViewModel(List<Baker> bakers, int currentCycle)
        {
            var baseUri = "https://api.baking-bad.org/";
            var result = bakers.Where(x => x.rating.status != 2 && x.rating.status != 6).OrderByDescending(x => (x.insurance?.coverage ?? 0)).ThenByDescending(y => y.rating.actualRoi).Select(x => new BakerViewModel
            {
                Address = x.address,
                Logo = $"{baseUri}/logos/{x.logo}",
                Name = x.name,
                Fee = x.config.fee.FirstOrDefault(y => y.cycle <= currentCycle)?.value ?? 0
            });

            return result;
        }

        public class Baker
        {
            public string address { get; set; }
            public string name { get; set; }
            public string logo { get; set; }
            public string site { get; set; }
            public decimal balance { get; set; }
            public decimal stakingBalance { get; set; }
            public decimal stakingCapacity { get; set; }
            public decimal estimatedRoi { get; set; }
            public Config config { get; set; }
            public Rating rating { get; set; }
            public Insurance insurance { get; set; }

        }

        public class Config
        {
            public string address { get; set; }
            public List<ConfigValue<decimal>> fee { get; set; }
            public List<ConfigValue<decimal>> minBalance { get; set; }
            public List<ConfigValue<bool>> payoutFee { get; set; }
            public List<ConfigValue<int>> payoutDelay { get; set; }
            public List<ConfigValue<int>> payoutPeriod { get; set; }
            public List<ConfigValue<decimal>> minPayout { get; set; }
            public List<ConfigValue<int>> rewardStruct { get; set; }
            public List<ConfigValue<decimal>> payoutRatio { get; set; }
            public List<string> ignored { get; set; }
            public List<string> sources { get; set; }
        }
        public class Rating
        {
            public string address { get; set; }
            public string delegator { get; set; }
            public string sharedConfig { get; set; }
            public int fromCycle { get; set; }
            public int toCycle { get; set; }
            public decimal avgRolls { get; set; }
            public decimal actualRoi { get; set; }
            public decimal prevRoi { get; set; }
            public int status { get; set; }
        }
        public class Insurance
        {
            public string address { get; set; }
            public string insuranceAddress { get; set; }
            public decimal insuranceAmount { get; set; }
            public decimal coverage { get; set; }
        }

        public class ConfigValue<T>
        {
            public int cycle { get; set; }
            public T value { get; set; }
        }

        public void Show()
        {
            Navigation.Navigate(
                uri: Navigation.DelegateAlias,
                context: this);
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
        }
    }
}