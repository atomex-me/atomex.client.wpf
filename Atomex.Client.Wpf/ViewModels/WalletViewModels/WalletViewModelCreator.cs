using System;

using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public static class WalletViewModelCreator
    {
        public static IWalletViewModel CreateViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel,
            CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ or
                Erc20Config _ or
                EthereumConfig _ => new WalletViewModel(
                    app: app,
                    dialogViewer: dialogViewer,
                    menuSelector: menuSelector,
                    conversionViewModel: conversionViewModel,
                    currency: currency),

                Fa12Config _ => new Fa12WalletViewModel(
                    app: app,
                    dialogViewer: dialogViewer,
                    menuSelector: menuSelector,
                    conversionViewModel: conversionViewModel,
                    currency: currency),

                TezosConfig _ => new TezosWalletViewModel(
                    app: app,
                    dialogViewer: dialogViewer,
                    menuSelector: menuSelector,
                    conversionViewModel: conversionViewModel,
                    currency: currency),

                _ => throw new NotSupportedException($"Can't create wallet view model for {currency.Name}. This currency is not supported."),
            };
        }
    }
}