using System;

using Atomex.Client.Wpf.Controls;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ => (SendViewModel)new BitcoinBasedSendViewModel(app, dialogViewer, currency),
                Erc20Config _        => (SendViewModel)new Erc20SendViewModel(app, dialogViewer, currency),
                EthereumConfig _     => (SendViewModel)new EthereumSendViewModel(app, dialogViewer, currency),
                Fa2Config _          => (SendViewModel)new FA2SendViewModel(app, dialogViewer, currency),
                Fa12Config _         => (SendViewModel)new Fa12SendViewModel(app, dialogViewer, currency),
                TezosConfig _        => (SendViewModel)new TezosSendViewModel(app, dialogViewer, currency),
                _ => throw new NotSupportedException($"Can't create send view model for {currency.Name}. This currency is not supported."),
            };
        }

        public static int GetSendPageId(CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ => Pages.SendBitcoinBased,
                Erc20Config _        => Pages.SendErc20,
                EthereumConfig _     => Pages.SendEthereum,
                Fa2Config _          => Pages.SendFA2,
                Fa12Config _         => Pages.SendFa12,
                TezosConfig _        => Pages.SendTezos,
                _ => throw new NotSupportedException($"Can't get send page id for currency {currency.Name}. This currency is not supported."),
            };
        }
    }
}