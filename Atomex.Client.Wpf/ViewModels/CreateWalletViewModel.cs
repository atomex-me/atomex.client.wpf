using System;
using System.Collections.Generic;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.ViewModels.CreateWalletViewModels;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public enum CreateWalletScenario
    {
        CreateNew,
        Restore
    }

    public class CreateWalletViewModel : BaseViewModel
    {
        private const int WalletTypeViewIndex = 0;
        private const int WalletNameViewIndex = 1;
        private const int CreateMnemonicViewIIndex = 2;
        private const int WriteMnemonicViewIIndex = 3;
        private const int CreateDerivedKeyPasswordViewIIndex = 4;
        private const int WriteDerivedKeyPasswordViewIIndex = 5;
        private const int CreateStoragePasswordViewIIndex = 6;

        public event Action<IAccount> OnAccountCreated;
        public event Action OnCanceled;

        private IAtomexApp App { get; }

        public List<StepViewModel> ViewModels { get; }

        private int[] CreateNewWalletViewIndexes { get; } = {
            WalletTypeViewIndex,
            WalletNameViewIndex,
            CreateMnemonicViewIIndex,
            CreateDerivedKeyPasswordViewIIndex,
            CreateStoragePasswordViewIIndex
        };

        private int[] RestoreWalletViewIndexes { get; } = {
            WalletTypeViewIndex,
            WalletNameViewIndex,
            WriteMnemonicViewIIndex,
            WriteDerivedKeyPasswordViewIIndex,
            CreateStoragePasswordViewIIndex
        };

        private int[] ViewIndexes { get; }

        private int _currentViewIndex;
        public int CurrentViewIndex
        {
            get => _currentViewIndex;
            set { _currentViewIndex = value; OnPropertyChanged(nameof(CurrentViewIndex)); }
        }

        private bool _canBack;
        public bool CanBack
        {
            get => _canBack;
            set { _canBack = value; OnPropertyChanged(nameof(CanBack)); }
        }

        private bool _canNext;
        public bool CanNext
        {
            get => _canNext;
            set { _canNext = value; OnPropertyChanged(nameof(CanNext)); }
        }

        private string _backText = Properties.Resources.CwvBack;
        public string BackText
        {
            get => _backText;
            set { _backText = value; OnPropertyChanged(nameof(BackText)); }
        }

        private string _nextText = Properties.Resources.CwvNext;
        public string NextText
        {
            get => _nextText;
            set { _nextText = value; OnPropertyChanged(nameof(NextText)); }
        }

        private int _step;
        public int Step
        {
            get => _step;
            set
            {
                _step = value;
                OnPropertyChanged(nameof(Step));

                if (_step < 0 || _step >= ViewIndexes.Length)
                    return;
            
                CurrentViewIndex = ViewIndexes[_step];
                ViewModels[CurrentViewIndex].Step = _step + 1;               

                NextText = _step < ViewIndexes.Length - 1
                    ? Properties.Resources.CwvNext
                    : Properties.Resources.CwvFinish;
            }
        }

        private int _stepsCount;
        public int StepsCount
        {
            get => _stepsCount;
            set { _stepsCount = value; OnPropertyChanged(nameof(StepsCount)); }
        }

        private bool _inProgress;
        public bool InProgress
        {
            get => _inProgress;
            set { _inProgress = value; OnPropertyChanged(nameof(InProgress)); }
        }

        public CreateWalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public CreateWalletViewModel(
            IAtomexApp app,
            CreateWalletScenario scenario,
            Action<IAccount> onAccountCreated = null,
            Action onCanceled = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            ViewModels = new List<StepViewModel>
            {
                new WalletTypeViewModel(),
                new WalletNameViewModel(),
                new CreateMnemonicViewModel(),
                new WriteMnemonicViewModel(),
                new CreateDerivedKeyPasswordViewModel(),
                new WriteDerivedKeyPasswordViewModel(),
                new CreateStoragePasswordViewModel(App)
            };

            InProgress = false;

            if (onAccountCreated != null)
                OnAccountCreated += onAccountCreated;

            if (onCanceled != null)
                OnCanceled += onCanceled;

            ViewIndexes = ResolveViewIndexes(scenario);

            foreach (var viewIndex in ViewIndexes)
            {
                var viewModel = ViewModels[viewIndex];

                viewModel.OnBack += arg =>
                {
                    if (Step == 0) {
                        OnCanceled?.Invoke();
                        return;
                    }

                    Step--;
                };
                viewModel.OnNext += arg =>
                {
                    if (Step == ViewIndexes.Length - 1)
                    {
                        OnAccountCreated?.Invoke((IAccount) arg);
                        return;
                    }

                    Step++;
                    ViewModels[CurrentViewIndex].Initialize(arg);
                };
                viewModel.ProgressBarShow += () => { InProgress = true; };
                viewModel.ProgressBarHide += () => { InProgress = false; };
            }

            Step = 0;
            StepsCount = ViewIndexes.Length;
            CanBack = true;
            CanNext = true;
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            ViewModels[CurrentViewIndex].Back();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(() =>
        {
            ViewModels[CurrentViewIndex].Next();
        }));

        private int[] ResolveViewIndexes(
            CreateWalletScenario scenario)
        {
            if (scenario == CreateWalletScenario.CreateNew)
                return CreateNewWalletViewIndexes;

            if (scenario == CreateWalletScenario.Restore)
                return RestoreWalletViewIndexes;

            throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        private void DesignerMode()
        {
            InProgress = false;
        }
    }
}