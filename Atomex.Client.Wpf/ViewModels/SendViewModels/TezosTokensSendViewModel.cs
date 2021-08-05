using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.WalletViewModels;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel 
    {
        private IAtomexApp _app;
        private IDialogViewer _dialogViewer;

        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public TezosTokenViewModel Token { get; set; }
        public ObservableCollection<string> FromAddresses { get; set; }
        public string From { get; set; }
        public string TokenContract { get; set; }
        public decimal TokenId { get; set; }
        public string To { get; set; }

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

            From          = from;
            TokenContract = tokenContract;
            TokenId       = tokenId;
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            _dialogViewer.HideDialog(Dialogs.Send);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextCommand);

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

        private void DesignerMode()
        {

        }
    }
}