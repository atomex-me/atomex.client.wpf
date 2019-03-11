using System;
using System.Collections.Generic;
using System.Linq;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class UseOfMultipleDevicesViewModel : StageViewModel
    {
        public const int MaxDeviceCount = 10;
        public const int DefaultDeviceCount = 1;

        public static IEnumerable<int> DevicesCount { get; } = Enumerable.Range(1, MaxDeviceCount);

        public override event Action<object> OnNext;

        //private HdWalletOld _wallet;

        private int _deviceCount = DefaultDeviceCount;
        public int DeviceCount
        {
            get => _deviceCount;
            set {
                var maxDeviceCount = DevicesCount.Last();

                _deviceCount = value <= maxDeviceCount
                    ? value
                    : maxDeviceCount;

                OnPropertyChanged(nameof(DeviceCount));

                DeviceIndexes = Enumerable.Range(0, _deviceCount);
            }
        }

        private IEnumerable<int> _deviceIndexes = Enumerable.Range(0, DefaultDeviceCount);
        public IEnumerable<int> DeviceIndexes
        {
            get => _deviceIndexes;
            set {
                _deviceIndexes = value;
                OnPropertyChanged(nameof(DeviceIndexes));

                var maxDeviceIndex = _deviceIndexes.Last();

                if (DeviceIndex > maxDeviceIndex)
                    DeviceIndex = maxDeviceIndex;
            }
        }

        private int _deviceIndex;
        public int DeviceIndex
        {
            get => _deviceIndex;
            set {
                _deviceIndex = value;
                OnPropertyChanged(nameof(DeviceIndex));
            }
        }

        public override void Initialize(object o)
        {
            //_wallet = (HdWalletOld) o;
        }

        public override void Next()
        {
            //_wallet.DeviceCount = _deviceCount;
            //_wallet.DeviceIndex = _deviceIndex;

            //OnNext?.Invoke(_wallet);
        }
    }
}