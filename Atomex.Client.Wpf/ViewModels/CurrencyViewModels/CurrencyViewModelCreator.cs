using System;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
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
            var result = currency switch
            {
                Bitcoin _ => (CurrencyViewModel)new BitcoinCurrencyViewModel(currency),
                Litecoin _ => (CurrencyViewModel)new LitecoinCurrencyViewModel(currency),
                Ethereum _ => (CurrencyViewModel)new EthereumCurrencyViewModel(currency),
                Tezos _ => (CurrencyViewModel)new TezosCurrencyViewModel(currency),
                _ => throw new NotSupportedException(
                    $"Can't create currency view model for {currency.Name}. This currency is not supported.")
            };

            if (!subscribeToUpdates)
                return result;

            result.SubscribeToUpdates(App.AtomexApp.Account);
            result.SubscribeToRatesProvider(App.AtomexApp.QuotesProvider);
            result.UpdateInBackgroundAsync();

            return result;
        }
    }
}