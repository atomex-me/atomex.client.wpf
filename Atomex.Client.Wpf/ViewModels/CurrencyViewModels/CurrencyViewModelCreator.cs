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
            CurrencyConfig currencyConfig,
            bool subscribeToUpdates)
        {
            var result = currencyConfig.Name switch
            {
                "BTC"   => (CurrencyViewModel)new BitcoinCurrencyViewModel(currencyConfig),
                "LTC"   => new LitecoinCurrencyViewModel(currencyConfig),
                "USDT"  => new TetherCurrencyViewModel(currencyConfig),
                "TBTC"  => new TbtcCurrencyViewModel(currencyConfig),
                "WBTC"  => new WbtcCurrencyViewModel(currencyConfig),
                "ETH"   => new EthereumCurrencyViewModel(currencyConfig),
                "TZBTC" => new TzbtcCurrencyViewModel(currencyConfig),
                "KUSD"  => new KusdCurrencyViewModel(currencyConfig),
                "XTZ"   => new TezosCurrencyViewModel(currencyConfig),
                _ => throw new NotSupportedException(
                    $"Can't create currency view model for {currencyConfig.Name}. This currency is not supported.")
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