using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfHomeNet
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _executeMethod;
        private readonly Func<object, bool> _canExecuteMethod;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object> executeMethod)
            : this(executeMethod, _ => true)
        {
        }

        public RelayCommand(Action<object> executeMethod, Func<object, bool> canExecuteMethod)
        {
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
            _canExecuteMethod = canExecuteMethod ?? throw new ArgumentNullException(nameof(canExecuteMethod));
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteMethod?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _executeMethod?.Invoke(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }


}
