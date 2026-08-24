using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PoderJudicial.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _ejecutar;
        private readonly Func<object, bool> _puedeEjecutar;

        public RelayCommand(Action<object> ejecutar, Func<object, bool> puedeEjecutar = null)
        {
            _ejecutar = ejecutar;
            _puedeEjecutar = puedeEjecutar;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) =>
            _puedeEjecutar == null || _puedeEjecutar(parameter);

        public void Execute(object parameter) => _ejecutar(parameter);
    }
}
