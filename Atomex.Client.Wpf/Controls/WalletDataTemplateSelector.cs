using System;
using System.Windows;
using System.Windows.Controls;

using Atomex.Client.Wpf.ViewModels.WalletViewModels;

namespace Atomex.Client.Wpf.Controls
{
    public class WalletDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate TezosTemplate { get; set; }
        public DataTemplate TezosTokensTemplate { get; set; }
        public DataTemplate Fa12Template { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is IWalletViewModel viewModel))
                return null;

            return viewModel switch
            {
                TezosWalletViewModel _     => TezosTemplate,
                TezosTokensWalletViewModel => TezosTokensTemplate,
                Fa12WalletViewModel _      => Fa12Template,
                IWalletViewModel _         => DefaultTemplate,
                _ => throw new NotSupportedException("Not supported view model type")
            };
        }
    }
}