using System;
using System.Windows.Input;

namespace ABClient.ViewModel
{
    public class ReallyCommand:ICommand
    {
        private readonly Action<object> _action;
        private readonly Func<object,bool> _func;

        public ReallyCommand(Action action)
        {
            _action= v => { action(); };
        }

        public ReallyCommand(Action<object> action)
        {
            _action = action;
        }



        public ReallyCommand(Action<object> action, Func<object, bool> func)
        {
            _action = action;
            _func = func;
        }

        public bool CanExecute(object parameter)
        {
            return _func == null || _func(parameter);
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }


        public event EventHandler CanExecuteChanged;

    }
}
