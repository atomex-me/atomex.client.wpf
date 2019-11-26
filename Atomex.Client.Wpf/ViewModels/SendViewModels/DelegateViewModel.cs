using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Common;
using Newtonsoft.Json;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }

        private List<BakerViewModel> _fromBakersList;
        public List<BakerViewModel> FromBakersList
        {
            get => _fromBakersList;
            private set { _fromBakersList = value; OnPropertyChanged(nameof(FromBakersList)); }
        }

        private BakerViewModel _bakerViewModel;
        public BakerViewModel BakerViewModel
        {
            get => _bakerViewModel;
            set
            {
                _bakerViewModel = value;
                Address = _bakerViewModel?.Address;
            }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { _fee = value; OnPropertyChanged(nameof(Fee)); }
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
            //if (string.IsNullOrEmpty(To)) {
            //    Warning = Resources.SvEmptyAddressError;
            //    return;
            //}

            //if (!Currency.IsValidAddress(To)) {
            //    Warning = Resources.SvInvalidAddressError;
            //    return;
            //}

            //if (Amount <= 0) {
            //    Warning = Resources.SvAmountLessThanZeroError;
            //    return;
            //}

            //if (Fee < 0) {
            //    Warning = Resources.SvCommissionLessThanZeroError;
            //    return;
            //}

            //if (Amount + Currency.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount) {
            //    Warning = Resources.SvAvailableFundsError;
            //    return;
            //}

            //var confirmationViewModel = new SendConfirmationViewModel(DialogViewer)
            //{
            //    Currency = Currency,
            //    To = To,
            //    Amount = Amount,
            //    AmountInBase = AmountInBase,
            //    BaseCurrencyCode = BaseCurrencyCode,
            //    BaseCurrencyFormat = BaseCurrencyFormat,
            //    Fee = Fee,
            //    FeeInBase = FeeInBase,
            //    FeePrice = FeePrice,
            //    CurrencyCode = CurrencyCode,
            //    CurrencyFormat = CurrencyFormat
            //};

            //Navigation.Navigate(
            //    uri: Navigation.SendConfirmationAlias,
            //    context: confirmationViewModel);
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

            LoadBakerList().FireAndForget();
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
                // todo: error to log
            }

            if (Application.Current.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FromBakersList = bakers;
                }, DispatcherPriority.Background);
            }
        }

        private async Task<IEnumerable<BakerViewModel>> GetBakers(CancellationToken cancellationToken = default)
        {
            var baseUri = "https://api.baking-bad.org/";

            var xtz = App.Account.Currencies.Get<Tezos>();
            
            var rpc = new Rpc(xtz.RpcNodeUri);

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
            var result = bakers.OrderByDescending(x => (x.insurance?.coverage ?? 0)).ThenByDescending(y => y.rating.actualRoi).Select(x => new BakerViewModel
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