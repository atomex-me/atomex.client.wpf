using System;
using System.Collections.Generic;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.ViewModels.HdWalletViewModels;
using Atomix.Wallet.Abstract;

namespace Atomix.Client.Wpf.ViewModels
{
    public enum CreateWalletScenario
    {
        CreateNewStages,
        UseMnemonicPhraseStages,
    }

    public class CreateWalletViewModel : BaseViewModel
    {
        private static class CreateWalletStage
        {
            public const int WalletName = 0;
            public const int CreateMnemonicPhrase = 1;
            public const int WriteMnemonicPhrase = 2;
            public const int CreateDerivedKeyPassword = 3;
            public const int WriteDerivedKeyPassword = 4;
            public const int CreateStoragePassword = 5;
            //public const int UseOfMultipleDevices = 5;
            public const int BlockchainScan = 6;
        }

        private int[] CreateNewStages { get; } =
        {
            CreateWalletStage.WalletName,
            CreateWalletStage.CreateMnemonicPhrase,
            CreateWalletStage.CreateDerivedKeyPassword,
            CreateWalletStage.CreateStoragePassword,
            //CreateWalletStage.UseOfMultipleDevices,

        };

        private int[] UseMnemonicPhraseStages { get; } =
        {
            CreateWalletStage.WalletName,
            CreateWalletStage.WriteMnemonicPhrase,
            CreateWalletStage.WriteDerivedKeyPassword,
            CreateWalletStage.CreateStoragePassword,
            //CreateWalletStage.UseOfMultipleDevices,
            //CreateWalletStage.BlockchainScan
        };

        public event Action<IAccount> OnAccountCreated;
        public event Action OnCanceled;

        public List<StageViewModel> Stages { get; set; } = new List<StageViewModel>
        {
            new WalletNameViewModel(),
            new CreateMnemonicViewModel(),
            new WriteMnemonicViewModel(),
            new CreateDerivedKeyPasswordViewModel(),
            new WriteDerivedKeyPasswordViewModel(),
            new CreateStoragePasswordViewModel(),
            //new UseOfMultipleDevicesViewModel(),
            new BlockchainScanViewModel()
        };

        private int[] _scenario;
        public int[] Scenario
        {
            get => _scenario;
            set
            {
                _scenario = value;
                StepsCount = _scenario?.Length ?? 0;

                if (_scenario != null)
                    Step = 0;
            }
        }

        private int _stage;
        public int Stage
        {
            get => _stage;
            set { _stage = value; OnPropertyChanged(nameof(Stage)); }
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

                Stage = Scenario[_step];

                if (_step < 0)
                    Scenario = null;

                CanBack = Scenario != null;
                CanNext = Scenario != null && _step < Scenario.Length;

                if (Scenario != null)
                    NextText = _step < Scenario.Length - 1
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
            CreateWalletScenario scenario,
            Action<IAccount> onAccountCreated = null,
            Action onCanceled = null)
        {
            InProgress = false;

            if (onAccountCreated != null)
                OnAccountCreated += onAccountCreated;

            if (onCanceled != null)
                OnCanceled += onCanceled;

            switch (scenario)
            {
                case CreateWalletScenario.CreateNewStages:
                    Scenario = CreateNewStages;
                    break;
                case CreateWalletScenario.UseMnemonicPhraseStages:
                    Scenario = UseMnemonicPhraseStages;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }

            foreach (var stage in Stages)
            {
                stage.OnBack += o =>
                {
                    if (Step == 0) {
                        OnCanceled?.Invoke();
                        return;
                    }

                    Step--;
                };
                stage.OnProgressShow += () => { InProgress = true; };
                stage.OnProgressHide += () => { InProgress = false; };
                stage.OnNext += o =>
                {
                    if (Step == Scenario.Length - 1) {
                        OnAccountCreated?.Invoke((IAccount) o);
                        return;
                    }

                    Step++;
                    Stages[Scenario[Step]].Initialize(o);
                };
            }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            Stages[Stage].Back();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(() =>
        {
            Stages[Stage].Next();
        }));

        private void DesignerMode()
        {
            InProgress = false;
        }
    }
}