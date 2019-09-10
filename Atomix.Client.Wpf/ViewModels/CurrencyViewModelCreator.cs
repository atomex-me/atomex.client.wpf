using System;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomix.Core.Entities;

namespace Atomix.Client.Wpf.ViewModels
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

            result.SubscribeToUpdates(App.AtomixApp.Account);
            result.SubscribeToRatesProvider(App.AtomixApp.QuotesProvider);
            result.UpdateInBackgroundAsync();

            return result;
        }
    }
}