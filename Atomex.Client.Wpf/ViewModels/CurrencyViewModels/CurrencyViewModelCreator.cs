using System;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public static class CurrencyViewModelCreator
    {
        public static CurrencyViewModel CreateViewModel(CurrencyConfig currency)
        {
            return CreateViewModel(currency, subscribeToUpdates: true);
        }

        public static CurrencyViewModel CreateViewModel(
            CurrencyConfig currency,
            bool subscribeToUpdates)
        {
            var result = currency.Name switch
            {
                "BTC"   => (CurrencyViewModel)new BitcoinCurrencyViewModel(currency),
                "LTC"   => new LitecoinCurrencyViewModel(currency),
                "USDT"  => new TetherCurrencyViewModel(currency),
                "TBTC"  => new TbtcCurrencyViewModel(currency),
                "WBTC"  => new WbtcCurrencyViewModel(currency),
                "ETH"   => new EthereumCurrencyViewModel(currency),
                "NYX"   => new NYXCurrencyViewModel(currency),
                "FA2"   => new FA2CurrencyViewModel(currency),
                "TZBTC" => new TzbtcCurrencyViewModel(currency),
                "KUSD"  => new KusdCurrencyViewModel(currency),
                "XTZ"   => new TezosCurrencyViewModel(currency),
                "FA12"  => new TzbtcCurrencyViewModel(currency),
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