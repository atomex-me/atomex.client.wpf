using System;
using System.Windows.Input;

namespace Atomex.Client.Wpf.Common
{
    public class Command : ICommand
    {
        private readonly Action _action;
        private readonly bool _canExecute;

        public Command(Action action)
            : this(action, true)
        {
        }

        public Command(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameter)
        {
            _action();
        }
    }
}