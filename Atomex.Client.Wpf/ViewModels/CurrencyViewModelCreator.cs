using System;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels
{
    public static class CurrencyViewModelCreator
    {
        public static CurrencyViewModel CreateViewModel(Currency currency)
        {
            return CreateViewModel(currency, subscribeToUpdates: true);
        }

        public static CurrencyViewModel CreateViewModel(
            Currency currency,
            bool subscribeToUpdates)
        {
            CurrencyViewModel result = null;

            if (currency is Bitcoin)
            {
                result = new BitcoinCurrencyViewModel(currency);
            }
            else if (currency is Litecoin)
            {
                result = new LitecoinCurrencyViewModel(currency);
            }
            else if (currency is Ethereum)
            {
                result = new EthereumCurrencyViewModel(currency);
            }
            else if (currency is Tezos)
            {
                result = new TezosCurrencyViewModel(currency);
            }

            if (result == null)
                throw new NotSupportedException(
                    $"Can't create currency view model for {currency.Name}. This currency is not supported.");

            if (!subscribeToUpdates)
                return result;

            result.SubscribeToUpdates(App.AtomexApp.Account);
            result.SubscribeToRatesProvider(App.AtomexApp.QuotesProvider);
            result.UpdateInBackgroundAsync();

            return result;
        }
    }
}