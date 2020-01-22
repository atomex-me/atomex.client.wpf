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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is WalletViewModel viewModel))
                return null;

            return viewModel switch
            {
                TezosWalletViewModel _ => TezosTemplate,
                WalletViewModel _ => DefaultTemplate,
                _ => throw new NotSupportedException("Not supported view model type")
            };
        }
    }
}