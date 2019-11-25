using Atomex.Core.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.MarketData.Abstract;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Common;

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
        private BakerViewModel BakerViewModel
        {
            get => _bakerViewModel;
            set
            {
                _bakerViewModel = value;

                Address = _bakerViewModel?.Address;
                Fee = _bakerViewModel.Fee;
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
            DialogViewer?.HideSendDialog();
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
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public DelegateViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        public void Show()
        {
            Navigation.Navigate(
                uri: Navigation.SendAlias,
                context: this);
        }

        private void DesignerMode()
        {
            _fromBakersList = new List<BakerViewModel>()
            {
                new BakerViewModel()
                {
                    Logo = "tezoshodl.png",
                    Name = "TezosHODL",
                    Address = "tz1sdfldjsflksjdlkf123sfa",
                    Fee = 5
                }
            };

            _address = "tz1sdfldjsflksjdlkf123sfa";
            _fee = 5;        
        }
    }
}