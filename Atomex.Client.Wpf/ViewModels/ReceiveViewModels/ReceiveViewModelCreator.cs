using System;
using Atomex.Client.Wpf.Controls;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Wpf.ViewModels.ReceiveViewModels
{
    public static class ReceiveViewModelCreator
    {
        public static ReceiveViewModel CreateViewModel(
            IAtomexApp app,
            Currency currency)
        {
            return currency switch
            {
                BitcoinBasedCurrency _ => new ReceiveViewModel(app, currency),
                ERC20 _ => new ReceiveViewModel(app, currency),
                Ethereum _ => new EthereumReceiveViewModel(app, currency),
                NYX _ => new ReceiveViewModel(app, currency),
                FA2 _ => new ReceiveViewModel(app, currency),
                FA12 _ => new ReceiveViewModel(app, currency),
                Tezos _ => new TezosReceiveViewModel(app, currency), 
                _ => throw new NotSupportedException($"Can't create receive view model for {currency.Name}. This currency is not supported."),
            };
        }
    }
}
