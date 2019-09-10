using System.Windows;
using System.Windows.Interactivity;

namespace Atomex.Client.Wpf.Behaviors
{
    public class StylizedBehaviorCollection : FreezableCollection<Behavior>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new StylizedBehaviorCollection();
        }
    }
}