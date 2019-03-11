using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Atomix.Core.Entities;
using Atomix.Wallet;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Properties;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class CurrencyItem : BaseViewModel
    {
        private readonly CurrencyViewModel _currencyViewModel;

        public Action<CurrencyItem> OnScan { get; set; }

        public Currency Currency => _currencyViewModel.Currency;
        public string IconPath => _currencyViewModel.IconPath;
        public string Header => _currencyViewModel.Header;
        public decimal AvailableAmount => _currencyViewModel.AvailableAmount;
        public string CurrencyFormat => _currencyViewModel.CurrencyFormat;
        public string CurrencyCode => _currencyViewModel.CurrencyCode;
        //public int DeviceIndex { get; set; }
        public CancellationTokenSource Cancellation { get; set; }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set {
                _isScanning = value;
                OnPropertyChanged(nameof(IsScanning));

                ScanText = _isScanning
                    ? Resources.CwvCancel
                    : Resources.CwvScan;

                IsEnabled = !_isScanning;
            }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        private string _scanText = Resources.CwvScan;
        public string ScanText
        {
            get => _scanText;
            set { _scanText = value; OnPropertyChanged(nameof(ScanText)); }
        }

        private int _freeKeyIndex;
        public int FreeKeyIndex
        {
            get => _freeKeyIndex;
            set { _freeKeyIndex = value; OnPropertyChanged(nameof(FreeKeyIndex)); }
        }

        private int _manualIndex;
        public int ManualIndex
        {
            get => _manualIndex;
            set { _manualIndex = value; OnPropertyChanged(nameof(ManualIndex)); }
        }

        public CurrencyItem(CurrencyViewModel currencyViewModel)
        {
            _currencyViewModel = currencyViewModel;
        }

        private ICommand _scanCommand;
        public ICommand ScanCommand => _scanCommand ?? (_scanCommand = new Command(() => OnScan?.Invoke(this)));
    }

    public class AccountTabItem : BaseViewModel
    {
        public string Header { get; set; }
        public List<CurrencyItem> Currencies { get; set; }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }
    }

    public class BlockchainScanViewModel : StageViewModel
    {
        private HdWallet Wallet { get; set; }
        private HdWalletScanner WalletScanner { get; set; }
        //private ITransactionStorage TransactionStorage { get; set; }

        public List<AccountTabItem> Tabs { get; set; }
        public override event Action<object> OnBack;
        public override event Action<object> OnNext;

        private CancellationTokenSource Cancellation { get; set; }

        private bool _isScanningAll;
        public bool IsScanningAll
        {
            get => _isScanningAll;
            set
            {
                _isScanningAll = value;
                OnPropertyChanged(nameof(IsScanningAll));

                ScanAllText = _isScanningAll
                    ? Resources.CwvCancel
                    : Resources.CwvScan;

                foreach (var deviceTab in Tabs)
                    deviceTab.IsEnabled = !_isScanningAll;
            }
        }

        private string _scanAllText = Resources.CwvScanAll;
        public string ScanAllText
        {
            get => _scanAllText;
            set { _scanAllText = value; OnPropertyChanged(nameof(ScanAllText)); }
        }

        public BlockchainScanViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        private ICommand _scanAllCommand;
        public ICommand ScanAllCommand => _scanAllCommand ?? (_scanAllCommand = new Command(OnScanAllCommand));

        public override void Initialize(object o)
        {
            Wallet = (HdWallet) o;

            //TransactionStorage = new TransactionLocalFileStorage(Path.GetDirectoryName(Wallet.PathToWallet));
            //WalletScanner = new HdWalletScanner(Wallet, TransactionStorage);

            Tabs = new List<AccountTabItem>
            {
                new AccountTabItem
                {
                    Header = "Account 0",
                    Currencies = Wallet.Currencies
                        .Where(c => c.IsTransactionsAvailable)
                        .Select(c => new CurrencyItem(CurrencyViewModelCreator.CreateViewModel(c, false))
                        {
                            FreeKeyIndex = 0,
                            OnScan = OnScanCommand
                        })
                        .ToList()
                }
            };


            OnPropertyChanged(nameof(Tabs));
        }

        public override void Back()
        {
            CancelAllScans();
            OnBack?.Invoke(null);
        }

        public override void Next()
        {
            CancelAllScans();
            OnNext?.Invoke(Wallet);
        }

        private void CancelAllScans()
        {
            foreach (var accountTab in Tabs)
                foreach (var currencyItem in accountTab.Currencies)
                    if (currencyItem.IsScanning)
                        currencyItem.Cancellation.Cancel();

            if (IsScanningAll)
                Cancellation.Cancel();
        }

        private void ApplyManualIndex(CurrencyItem currencyItem)
        {
            //WalletScanner.
            //_wallet.SetFreeKeyIndex(currencyItem.DeviceIndex, currencyItem.Currency, currencyItem.ManualIndex);
        }

        private void ApplyManualIndexes()
        {
            foreach (var deviceTab in Tabs)
                foreach (var currencyItem in deviceTab.Currencies)
                    ApplyManualIndex(currencyItem);
        }

        private void RestoreFreeKeyIndex(CurrencyItem currencyItem)
        {
            //currencyItem.FreeKeyIndex = _wallet.GetFreeKeyIndex(currencyItem.DeviceIndex, currencyItem.Currency);
        }

        private void RestoreFreeKeyIndexes()
        {
            foreach (var deviceTab in Tabs)
                foreach (var currencyItem in deviceTab.Currencies)
                    RestoreFreeKeyIndex(currencyItem);
        }

        private void OnScanCommand(CurrencyItem ci)
        {
            if (!ci.IsScanning)
            {
                ci.Cancellation = new CancellationTokenSource();
                ci.IsScanning = true;
                ApplyManualIndex(ci);

                try
                {
                    //await _wallet.ScanAsync(
                    //    deviceIndex: ci.DeviceIndex,
                    //    currency: ci.Currency,
                    //    force: false,
                    //    lookAhead: HdWalletOld.DefaultScanLookAhead,
                    //    cancellationToken: ci.Cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Debug($"Blockchain scan canceled! (Currency: {ci.Currency.Name})");
                }
                catch (Exception e)
                {
                    //Log.Fatal($"Blockchain scan canceled! (Device: {ci.DeviceIndex}, Currency: {ci.Currency.Name})");
                    Console.WriteLine(e);
                    throw;
                }

                RestoreFreeKeyIndex(ci);
                ci.IsScanning = false;
            }
            else
            {
                if (!ci.Cancellation.IsCancellationRequested)
                    ci.Cancellation.Cancel();
            }
        }

        private void OnScanAllCommand()
        {
            if (!IsScanningAll)
            {
                CancelAllScans();

                Cancellation = new CancellationTokenSource();
                IsScanningAll = true;
                ApplyManualIndexes();

                try
                {
                    //await _wallet.ScanAsync(
                    //    lookAhead: HdWalletOld.DefaultScanLookAhead,
                    //    cancellationToken: Cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Debug("Blockchain scan canceled! (All currencies)");
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e);
                    //throw;
                }

                RestoreFreeKeyIndexes();
                IsScanningAll = false;
            }
            else
            {
                if (!Cancellation.IsCancellationRequested)
                    Cancellation.Cancel();
            }
        }

        private void DesignerMode()
        {
            Tabs = new List<AccountTabItem>
            {
                new AccountTabItem
                {
                    Header = "Account 0",
                    Currencies = new List<CurrencyItem>
                    {
                        new CurrencyItem(CurrencyViewModelCreator.CreateViewModel(Currencies.Btc, subscribeToUpdates: false)),
                        new CurrencyItem(CurrencyViewModelCreator.CreateViewModel(Currencies.Ltc, subscribeToUpdates: false))
                        {
                            FreeKeyIndex = 5                            
                        }
                    }
                },
                new AccountTabItem
                {
                    Header = "Account 1",
                    Currencies = new List<CurrencyItem>
                    {
                        new CurrencyItem(CurrencyViewModelCreator.CreateViewModel(Currencies.Btc, subscribeToUpdates: false))
                        {
                            FreeKeyIndex = 4
                        },
                        new CurrencyItem(CurrencyViewModelCreator.CreateViewModel(Currencies.Ltc, subscribeToUpdates: false))
                    }
                }
            };
        }
    }
}