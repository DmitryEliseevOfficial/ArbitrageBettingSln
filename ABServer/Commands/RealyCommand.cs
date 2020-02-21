using System;
using System.Windows.Input;

namespace ABServer.Commands
{
    internal class RealyCommand : ICommand
    {
        private readonly Action<object> _command;
        private readonly Func<object, bool> _func;

#pragma warning disable CS0067 // Событие "RealyCommand.CanExecuteChanged" никогда не используется.
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067 // Событие "RealyCommand.CanExecuteChanged" никогда не используется.

        public RealyCommand(Action command)
        {
            _command = delegate {
                command();
            };
        }

        public RealyCommand(Action<object> command)
        {
            _command = command;
        }

        public RealyCommand(Action<object> command,Func<object,bool> compaer)
        {
            _command = command;
            _func = compaer;
        }

        public bool CanExecute(object parameter)
        {
            return _func == null || _func(parameter);
        }

        public void Execute(object parameter)
        {
            _command(parameter);
        }
    }
}
