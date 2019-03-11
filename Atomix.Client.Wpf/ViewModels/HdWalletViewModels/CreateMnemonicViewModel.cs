using System;
using System.Collections.Generic;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Properties;
using Atomix.Cryptography;
using NBitcoin;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class CreateMnemonicViewModel : StageViewModel
    {
        public static IEnumerable<KeyValuePair<string, Wordlist>> Languages { get; } = new List<KeyValuePair<string, Wordlist>>
        {
            new KeyValuePair<string, Wordlist>("English", Wordlist.English),
            new KeyValuePair<string, Wordlist>("French", Wordlist.French),
            new KeyValuePair<string, Wordlist>("Japanese", Wordlist.Japanese),
            new KeyValuePair<string, Wordlist>("Spanish", Wordlist.Spanish),
            new KeyValuePair<string, Wordlist>("Portuguese Brazil", Wordlist.PortugueseBrazil),
            new KeyValuePair<string, Wordlist>("Chinese Traditional", Wordlist.ChineseTraditional),
            new KeyValuePair<string, Wordlist>("Chinese Simplified", Wordlist.ChineseSimplified)
        };

        public static IEnumerable<KeyValuePair<string, int>> WordCountToEntropyLength { get; } = new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("12", 128),
            new KeyValuePair<string, int>("15", 160),
            new KeyValuePair<string, int>("18", 192),
            new KeyValuePair<string, int>("21", 224),
            new KeyValuePair<string, int>("24", 256)
        };

        public override event Action<object> OnNext;

        private string PathToWallet { get; set; }

        private string _mnemonic;
        public string Mnemonic
        {
            get => _mnemonic;
            set { _mnemonic = value; OnPropertyChanged(nameof(Mnemonic)); }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private Wordlist _language = Wordlist.English;
        public Wordlist Language
        {
            get => _language;
            set
            {
                if (_language != value) {
                    _language = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private int _entropyLength = 192;
        public int EntropyLength
        {
            get => _entropyLength;
            set
            {
                if (_entropyLength != value) {
                    _entropyLength = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private ICommand _mnemonicCommand;
        public ICommand MnemonicCommand => _mnemonicCommand ?? (_mnemonicCommand = new Command(() =>
        {
            var entropy = Rand.SecureRandomBytes(EntropyLength / 8);

            Mnemonic = new Mnemonic(Language, entropy).ToString();
            Warning = string.Empty;
        }));

        public override void Initialize(object o)
        {
            PathToWallet = (string) o;
        }

        public override void Back()
        {
            Warning = string.Empty;
            base.Back();
        }

        public override void Next()
        {
            if (string.IsNullOrEmpty(Mnemonic)) {
                Warning = Resources.CwvCreateMnemonicWarning;
                return;
            }

            OnNext?.Invoke(new MnemonicStageData
            {
                Mnemonic = Mnemonic,
                Language = Language,
                PathToWallet = PathToWallet
            });
        }
    }
}