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
        public static WalletViewModel CreateViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel,
            Currency currency)
        {
            switch (currency)
            {
                case BitcoinBasedCurrency _:
                case FA12 _:
                case ERC20 _:
                case Ethereum _:
                    return new WalletViewModel(
                        app: app,
                        dialogViewer: dialogViewer,
                        menuSelector: menuSelector,
                        conversionViewModel: conversionViewModel,
                        currency: currency);
                case Tezos _:
                    return new TezosWalletViewModel(
                        app: app,
                        dialogViewer: dialogViewer,
                        menuSelector: menuSelector,
                        conversionViewModel: conversionViewModel,
                        currency: currency);
                default:
                    throw new NotSupportedException($"Can't create wallet view model for {currency.Name}. This currency is not supported.");

            }
        }
    }
}