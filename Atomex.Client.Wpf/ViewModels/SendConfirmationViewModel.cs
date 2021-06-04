using System;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Core;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class SendConfirmationViewModel : BaseViewModel
    {
        private readonly int _dialogId;
        private readonly IDialogViewer _dialogViewer;

        public CurrencyConfig Currency { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal FeePrice { get; set; }
        public decimal FeeAmount => Currency.GetFeeAmount(Fee, FeePrice);
        public bool UseDeafultFee { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }
        public string FeeCurrencyCode { get; set; }
        public string FeeCurrencyFormat { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            _dialogViewer.Back(_dialogId);
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(Send));

#if DEBUG
        public SendConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public SendConfirmationViewModel(IDialogViewer dialogViewer, int dialogId)
        {
            _dialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            _dialogId = dialogId;
        }

        private async void Send()
        {
            var account = App.AtomexApp.Account;

            try
            {
                _dialogViewer.PushPage(_dialogId, Pages.Sending);

                var error = await account
                    .SendAsync(Currency.Name, To, Amount, Fee, FeePrice, UseDeafultFee);

                if (error != null)
                {
                    _dialogViewer.PushPage(_dialogId, Pages.Message, MessageViewModel.Error(
                        text: error.Description,
                        backAction: BackToConfirmation));

                    return;
                }

                _dialogViewer.PushPage(_dialogId, Pages.Message, MessageViewModel.Success(
                    text: "Sending was successful",
                    nextAction: () => { _dialogViewer.HideDialog(_dialogId); }));
            }
            catch (Exception e)
            {
                _dialogViewer.PushPage(_dialogId, Pages.Message, MessageViewModel.Error(
                    text: "An error has occurred while sending transaction.",
                    backAction: BackToConfirmation));

                Log.Error(e, "Transaction send error.");
            }
        }

        private void BackToConfirmation()
        {
            _dialogViewer.Back(_dialogId); // to sending view
            _dialogViewer.Back(_dialogId); // to confirmation view
        }

        private void DesignerMode()
        {
            To = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Amount = 0.00001234m;
            AmountInBase = 10.23m;
            Fee = 0.0001m;
            FeePrice = 1m;
            FeeInBase = 8.43m;
            UseDeafultFee = true;
        }
    }
}