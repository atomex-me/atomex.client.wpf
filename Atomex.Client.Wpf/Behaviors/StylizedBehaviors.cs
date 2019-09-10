using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interactivity;

namespace Atomex.Client.Wpf.Behaviors
{
    public class StylizedBehaviors
    {
        public static readonly DependencyProperty BehaviorsProperty
            = DependencyProperty.RegisterAttached("Behaviors",
                                                  typeof(StylizedBehaviorCollection),
                                                  typeof(StylizedBehaviors),
                                                  new FrameworkPropertyMetadata(null, OnPropertyChanged));

        public static StylizedBehaviorCollection GetBehaviors(DependencyObject uie)
        {
            return (StylizedBehaviorCollection)uie.GetValue(BehaviorsProperty);
        }

        public static void SetBehaviors(DependencyObject uie, StylizedBehaviorCollection value)
        {
            uie.SetValue(BehaviorsProperty, value);
        }

        private static void OnPropertyChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e)
        {
            if (!(dpo is FrameworkElement uie))
                return;

            var newBehaviors = e.NewValue as StylizedBehaviorCollection;
            var oldBehaviors = e.OldValue as StylizedBehaviorCollection;
            if (Equals(newBehaviors, oldBehaviors))
                return;

            var itemBehaviors = Interaction.GetBehaviors(uie);

            uie.Unloaded -= FrameworkElementUnloaded;

            if (oldBehaviors != null)
            {
                foreach (var behavior in oldBehaviors)
                {
                    var index = GetIndexOf(itemBehaviors, behavior);
                    if (index >= 0)
                        itemBehaviors.RemoveAt(index);
                }
            }

            if (newBehaviors != null)
            {
                foreach (var behavior in newBehaviors)
                {
                    var index = GetIndexOf(itemBehaviors, behavior);
                    if (index < 0)
                    {
                        var clone = (Behavior)behavior.Clone();
                        SetOriginalBehavior(clone, behavior);
                        itemBehaviors.Add(clone);
                    }
                }
            }

            if (itemBehaviors.Count > 0)
                uie.Unloaded += FrameworkElementUnloaded;

            uie.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private static void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Dispatcher.ShutdownStarted");
        }

        private static void FrameworkElementUnloaded(object sender, RoutedEventArgs e)
        {
            // BehaviorCollection doesn't call Detach, so we do this
            if (!(sender is FrameworkElement uie))
                return;
            
            var itemBehaviors = Interaction.GetBehaviors(uie);
            foreach (var behavior in itemBehaviors)
                behavior.Detach();

            uie.Loaded += FrameworkElementLoaded;
        }

        private static void FrameworkElementLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement uie))
                return;

            uie.Loaded -= FrameworkElementLoaded;
            var itemBehaviors = Interaction.GetBehaviors(uie);
            foreach (var behavior in itemBehaviors)
                behavior.Attach(uie);
        }

        private static int GetIndexOf(BehaviorCollection itemBehaviors, Behavior behavior)
        {
            var index = -1;

            var orignalBehavior = GetOriginalBehavior(behavior);

            for (var i = 0; i < itemBehaviors.Count; i++)
            {
                var currentBehavior = itemBehaviors[i];
                if (Equals(currentBehavior, behavior) || Equals(currentBehavior, orignalBehavior))
                {
                    index = i;
                    break;
                }

                var currentOrignalBehavior = GetOriginalBehavior(currentBehavior);
                if (Equals(currentOrignalBehavior, behavior) || Equals(currentOrignalBehavior, orignalBehavior))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        private static readonly DependencyProperty OriginalBehaviorProperty
            = DependencyProperty.RegisterAttached("OriginalBehavior",
                                                  typeof(Behavior),
                                                  typeof(StylizedBehaviors),
                                                  new UIPropertyMetadata(null));

        private static Behavior GetOriginalBehavior(DependencyObject obj)
        {
            return obj.GetValue(OriginalBehaviorProperty) as Behavior;
        }

        private static void SetOriginalBehavior(DependencyObject obj, Behavior value)
        {
            obj.SetValue(OriginalBehaviorProperty, value);
        }
    }
}