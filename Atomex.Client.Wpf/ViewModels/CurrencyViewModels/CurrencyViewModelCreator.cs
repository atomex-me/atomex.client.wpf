using System;

using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

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
                "TZBTC" => new TzbtcCurrencyViewModel(currency),
                "KUSD"  => new KusdCurrencyViewModel(currency),
                "XTZ"   => new TezosCurrencyViewModel(currency),
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