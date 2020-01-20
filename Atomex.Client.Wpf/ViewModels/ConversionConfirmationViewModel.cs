using System;
using System.Globalization;
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
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ConversionConfirmationViewModel : BaseViewModel
    {
        private static TimeSpan SwapTimeout = TimeSpan.FromSeconds(60);
        private static TimeSpan SwapCheckInterval = TimeSpan.FromSeconds(3);

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
            DialogViewer.HideDialog(Dialogs.Convert);
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

        private async void Send()
        {
            try
            {
                DialogViewer.PushPage(Dialogs.Convert, Pages.Sending);

                var error = await ConvertAsync();

                if (error != null)
                {
                    if (error.Code == Errors.PriceHasChanged)
                    {
                        DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Message(
                            title: Resources.SvFailed,
                            text: error.Description,
                            backAction: BackToConfirmation));
                    }
                    else
                    {
                        DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Error(
                            text: error.Description,
                            backAction: BackToConfirmation));
                    }

                    return;
                }

                DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Success(
                    text: Resources.SvOrderMatched,
                    nextAction: () => { DialogViewer?.HideDialog(Dialogs.Convert); }));
            }
            catch (Exception e)
            {
                DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Error(
                    text: "An error has occurred while sending swap.",
                    backAction: BackToConfirmation));

                Log.Error(e, "Swap error.");
            }
        }

        private async Task<Error> ConvertAsync()
        {
            try
            {
                var account = App.Account;

                var fromWallets = (await account
                    .GetUnspentAddressesAsync(
                        toAddress: null,
                        currency: FromCurrency.Name,
                        amount: Amount,
                        fee: 0,
                        feePrice: 0,
                        feeUsagePolicy: FeeUsagePolicy.EstimatedFee,
                        addressUsagePolicy: AddressUsagePolicy.UseMinimalBalanceFirst,
                        transactionType: BlockchainTransactionType.SwapPayment))
                    .ToList();

                if (Amount == 0)
                    return new Error(Errors.SwapError, Resources.CvWrongAmount);

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, Resources.CvInsufficientFunds);

                var symbol   = App.Account.Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                var side     = symbol.OrderSideForBuyCurrency(ToCurrency);
                var terminal = App.Terminal;
                var price    = EstimatedPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, Resources.CvNoLiquidity);

                var qty = Math.Round(AmountHelper.AmountToQty(side, Amount, price), symbol.Base.Digits);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = Math.Round(AmountHelper.QtyToAmount(side, symbol.MinimumQty, price), FromCurrency.Digits);
                    var message = string.Format(CultureInfo.InvariantCulture, Resources.CvMinimumAllowedQtyWarning, minimumAmount, FromCurrency.Name);

                    return new Error(Errors.SwapError, message);
                }

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

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SwapTimeout)
                {
                    await Task.Delay(SwapCheckInterval);

                    var currentOrder = terminal.Account.GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                        return null;

                    if (currentOrder.Status == OrderStatus.Canceled)
                        return new Error(Errors.PriceHasChanged, Resources.SvPriceHasChanged);

                    if (currentOrder.Status == OrderStatus.Rejected)
                        return new Error(Errors.OrderRejected, Resources.SvOrderRejected);
                }

                return new Error(Errors.TimeoutReached, Resources.SvTimeoutReached);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");
            
                return new Error(Errors.SwapError, Resources.CvConversionError);
            }
        }

        private void BackToConfirmation()
        {
            DialogViewer.Back(Dialogs.Convert); // to sending view
            DialogViewer.Back(Dialogs.Convert); // to confirmation view
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