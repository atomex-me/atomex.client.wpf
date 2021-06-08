using System.Linq;

using Atomex.Core;
using Atomex.Client.Wpf.Common;
using Atomex.Common;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels.ReceiveViewModels
{
    public class EthereumReceiveViewModel : ReceiveViewModel
    {
        public override CurrencyConfig Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged(nameof(Currency));
#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    var activeTokenAddresses = App.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(_currency.Name)
                        .GetUnspentTokenAddressesAsync()
                        .WaitForResult()
                        .ToList();

                    var activeAddresses = App.Account
                        .GetUnspentAddressesAsync(_currency.Name)
                        .WaitForResult()
                        .ToList();

                    activeTokenAddresses.ForEach(a => a.Balance = activeAddresses.Find(b => b.Address == a.Address)?.Balance ?? 0m);

                    activeAddresses = activeAddresses
                        .Where(a => activeTokenAddresses.FirstOrDefault(b => b.Address == a.Address) == null)
                        .ToList();

                    var freeAddress = App.Account
                        .GetFreeExternalAddressAsync(_currency.Name)
                        .WaitForResult();

                    var receiveAddresses = activeTokenAddresses.Select(w => new WalletAddressViewModel(w, _currency.Format))
                        .Concat(activeAddresses.Select(w => new WalletAddressViewModel(w, _currency.Format)))
                        .ToList();
                    
                    if (receiveAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                        receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, _currency.Format, isFreeAddress: true));

                    FromAddressList = receiveAddresses;
#if DEBUG
                }
#endif
            }
        }

        public EthereumReceiveViewModel(IAtomexApp app)
            : base(app, null)
        {
        }

        public EthereumReceiveViewModel(IAtomexApp app, CurrencyConfig currency)
            : base(app, currency)
        {
        }

        protected override WalletAddress GetDefaultAddress()
        {
            var activeAddressViewModel = FromAddressList
                .FirstOrDefault(vm => vm.WalletAddress.HasActivity);

            if (activeAddressViewModel != null)
                return activeAddressViewModel.WalletAddress;

            return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        }
    }
}