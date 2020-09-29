using System;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

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
                Tether _ => (CurrencyViewModel)new TetherCurrencyViewModel(currency),
                TBTC _ => (CurrencyViewModel)new TbtcCurrencyViewModel(currency),
                WBTC _ => (CurrencyViewModel)new WbtcCurrencyViewModel(currency),
                Ethereum _ => (CurrencyViewModel)new EthereumCurrencyViewModel(currency),
                NYX _ => (CurrencyViewModel)new NYXCurrencyViewModel(currency),
                FA2 _ => (CurrencyViewModel)new FA2CurrencyViewModel(currency),
                TZBTC _ => (CurrencyViewModel)new TzbtcCurrencyViewModel(currency),
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