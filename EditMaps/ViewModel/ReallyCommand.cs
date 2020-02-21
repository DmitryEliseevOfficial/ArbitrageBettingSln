using System;
using System.Windows.Input;

namespace EditMaps.ViewModel
{
    public class ReallyCommand : ICommand
    {
        private readonly Action<object> _action;

        public ReallyCommand(Action<object> action)
        {
            _action = action;
        }

        public ReallyCommand(Action action)
        {
            _action = delegate { action(); };
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }
    }
}