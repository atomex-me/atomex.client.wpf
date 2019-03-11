using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Atomix.Client.Wpf.ViewModels.Abstract;

namespace Atomix.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class BitcoinBasedCurrencyViewModel : CurrencyViewModel
    {
        public override async Task UpdateAsync()
        {
            var outputs = await Account
                .GetUnspentOutputsAsync(Currency, skipUnconfirmed: false);

            var confirmedOutputs = await Account
                .GetUnspentOutputsAsync(Currency);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TotalAmount = outputs.Sum(o => o.Value / (decimal)Currency.DigitsMultiplier);
                OnPropertyChanged(nameof(TotalAmount));

                AvailableAmount = confirmedOutputs.Sum(o => o.Value / (decimal)Currency.DigitsMultiplier);
                OnPropertyChanged(nameof(AvailableAmount));

                UnconfirmedAmount = TotalAmount - AvailableAmount;
                OnPropertyChanged(nameof(UnconfirmedAmount));
                OnPropertyChanged(nameof(HasUnconfirmedAmount));

                UpdateQuotesInBaseCurrency(QuotesProvider);

            }, DispatcherPriority.Background);
        }
    }
}