using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HomeNetAvalonia.Behaviors
{
    public static class LostFocusCommandBehavior
    {
        // 🔥 Регистрируем AttachedProperty для команды
        public static readonly AttachedProperty<ICommand> CommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand>(
                "Command",
                typeof(LostFocusCommandBehavior));

        // Регистрируем обработчик изменения свойства
        static LostFocusCommandBehavior()
        {
            CommandProperty.Changed.AddClassHandler<Control>((control, args) =>
            {
                if (args.NewValue is ICommand)
                {
                    // Если команду установили — подписываемся на LostFocus
                    control.LostFocus -= OnLostFocus;
                    control.LostFocus += OnLostFocus;
                }
                else
                {
                    // Если команду убрали — отписываемся
                    control.LostFocus -= OnLostFocus;
                }
            });
        }

        public static ICommand GetCommand(Control element) => element.GetValue(CommandProperty);
        public static void SetCommand(Control element, ICommand value) => element.SetValue(CommandProperty, value);

        private static void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is Control control)
            {
                var command = GetCommand(control);
                if (command?.CanExecute(null) == true)
                {
                    command.Execute(null);
                }
            }
        }
    }
}