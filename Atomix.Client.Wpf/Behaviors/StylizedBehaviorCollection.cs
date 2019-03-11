using System.Windows;
using System.Windows.Interactivity;

namespace Atomix.Client.Wpf.Behaviors
{
    public class StylizedBehaviorCollection : FreezableCollection<Behavior>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new StylizedBehaviorCollection();
        }
    }
}