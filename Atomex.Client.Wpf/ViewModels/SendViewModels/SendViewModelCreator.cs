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
            Currency currency)
        {
            return currency switch
            {
                BitcoinBasedCurrency _ => (SendViewModel) new BitcoinBasedSendViewModel(app, dialogViewer, currency),
                FA12 _ => (SendViewModel) new Fa12SendViewModel(app, dialogViewer, currency),
                ERC20 _ => (SendViewModel) new Erc20SendViewModel(app, dialogViewer, currency),
                Ethereum _ => (SendViewModel) new EthereumSendViewModel(app, dialogViewer, currency),
                Tezos _ => (SendViewModel) new TezosSendViewModel(app, dialogViewer, currency),
                _ => throw new NotSupportedException($"Can't create send view model for {currency.Name}. This currency is not supported."),
            };
        }

        public static int GetSendPageId(Currency currency)
        {
            return currency switch
            {
                BitcoinBasedCurrency _ => Pages.SendBitcoinBased,
                FA12 _ => Pages.SendFa12,
                ERC20 _ => Pages.SendErc20,
                Ethereum _ => Pages.SendEthereum,
                Tezos _ => Pages.SendTezos,
                _ => throw new NotSupportedException($"Can't get send page id for currency {currency.Name}. This currency is not supported."),
            };
        }
    }
}