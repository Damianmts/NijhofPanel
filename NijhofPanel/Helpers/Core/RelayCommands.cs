using System.Windows.Input;

namespace NijhofPanel.Helpers.Core;

public static class RelayCommands
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null!)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter is T value ? value : default!) ?? true;

        public void Execute(object? parameter) =>
            _execute(parameter is T value ? value : default!);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand : RelayCommand<object?>
    {
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null!)
            : base(execute, canExecute)
        {
        }
    }
}