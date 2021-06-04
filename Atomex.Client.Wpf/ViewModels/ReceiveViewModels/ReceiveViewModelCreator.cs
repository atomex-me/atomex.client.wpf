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
            CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ => new ReceiveViewModel(app, currency),
                Erc20Config _ => new ReceiveViewModel(app, currency),
                EthereumConfig _ => new EthereumReceiveViewModel(app, currency),
                NyxConfig _ => new ReceiveViewModel(app, currency),
                Fa2Config _ => new ReceiveViewModel(app, currency),
                Fa12Config _ => new ReceiveViewModel(app, currency),
                TezosConfig _ => new TezosReceiveViewModel(app, currency), 
                _ => throw new NotSupportedException($"Can't create receive view model for {currency.Name}. This currency is not supported."),
            };
        }
    }
}
