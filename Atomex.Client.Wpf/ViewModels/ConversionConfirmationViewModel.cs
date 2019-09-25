using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.Core.Entities;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ConversionConfirmationViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        public Currency FromCurrency { get; set; }
        public Currency ToCurrency { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public string PriceFormat { get; set; }
        public string CurrencyFormat { get; set; }
        public string TargetCurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string TargetCurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedPaymentFee { get; set; }
        public decimal EstimatedPaymentFeeInBase { get; set; }
        public decimal EstimatedRedeemFee { get; set; }
        public decimal EstimatedRedeemFeeInBase { get; set; }
        public bool UseRewardForRedeem { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer?.HideConversionConfirmationDialog();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(Send));

#if DEBUG
        public ConversionConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public ConversionConfirmationViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        public void Show()
        {
            Navigation.Navigate(
                uri: Navigation.ConversionConfirmationAlias,
                context: this);
        }

        private async void Send()
        {
            try
            {
                Navigation.Navigate(uri: Navigation.SendingAlias);

                var error = await SendSwapAsync();

                if (error != null)
                {
                    Navigation.Navigate(
                        uri: Navigation.MessageAlias,
                        context: MessageViewModel.Error(
                            text: error.Description,
                            goBackPages: 2));
                    return;
                }

                Navigation.Navigate(
                    uri: Navigation.MessageAlias,
                    context: MessageViewModel.Success(
                        text: "Sending was successful",
                        nextAction: () => {DialogViewer?.HideConversionConfirmationDialog();}));
            }
            catch (Exception e)
            {
                Navigation.Navigate(
                    uri: Navigation.MessageAlias,
                    context: MessageViewModel.Error(
                        text: "An error has occurred while sending swap.",
                        goBackPages: 2));

                Log.Error(e, "Swap error.");
            }
        }

        private async Task<Error> SendSwapAsync()
        {
            try
            {
                var account = App.Account;

                var fromWallets = (await account
                    .GetUnspentAddressesAsync(
                        currency: FromCurrency,
                        amount: Amount,
                        fee: 0,
                        feePrice: 0,
                        feeUsagePolicy: FeeUsagePolicy.EstimatedFee,
                        addressUsagePolicy: AddressUsagePolicy.UseMinimalBalanceFirst,
                        transactionType: BlockchainTransactionType.SwapPayment))
                    .ToList();

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, Resources.CvInsufficientFunds);

                var symbol   = App.Account.Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                var side     = symbol.OrderSideForBuyCurrency(ToCurrency);
                var terminal = App.Terminal;
                var price    = EstimatedPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, Resources.CvNoLiquidity);

                var qty = Math.Round(AmountHelper.AmountToQty(side, Amount, price), symbol.Base.Digits);

                var order = new Order
                {
                    Symbol = symbol,
                    TimeStamp = DateTime.UtcNow,
                    Price = price,
                    Qty = qty,
                    Side = side,
                    Type = OrderType.FillOrKill,
                    FromWallets = fromWallets.ToList()
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");
            
                return new Error(Errors.SwapError, Resources.CvConversionError);
            }

            return null;
        }

        private void DesignerMode()
        {
            var currencies = DesignTime.Currencies;

            FromCurrency = currencies[0];
            ToCurrency = currencies[1];
            FromCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(FromCurrency, false);
            ToCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(ToCurrency, false);

            PriceFormat = $"F{FromCurrency.Digits}";
            CurrencyCode = FromCurrencyViewModel.CurrencyCode;
            CurrencyFormat = FromCurrencyViewModel.CurrencyFormat;
            TargetCurrencyCode = ToCurrencyViewModel.CurrencyCode;
            TargetCurrencyFormat = ToCurrencyViewModel.CurrencyFormat;
            BaseCurrencyCode = FromCurrencyViewModel.BaseCurrencyCode;
            BaseCurrencyFormat = FromCurrencyViewModel.BaseCurrencyFormat;

            EstimatedPrice = 0.003m;

            Amount = 0.00001234m;
            AmountInBase = 10.23m;

            TargetAmount = Amount / EstimatedPrice;
            TargetAmountInBase = AmountInBase;

            EstimatedPaymentFee = 0.0001904m;
            EstimatedPaymentFeeInBase = 0.22m;

            EstimatedRedeemFee = 0.001m;
            EstimatedRedeemFeeInBase = 0.11m;
        }
    }
}