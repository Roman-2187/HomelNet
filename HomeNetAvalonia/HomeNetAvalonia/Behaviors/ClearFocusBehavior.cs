using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace HomeNetAvalonia.Behaviors
{
    public static class ClearFocusBehavior
    {
        // 🔥 Регистрируем AttachedProperty
        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(ClearFocusBehavior), false);

        static ClearFocusBehavior()
        {
            IsEnabledProperty.Changed.AddClassHandler<Control>((control, args) =>
            {
                if (args.NewValue is bool isEnabled)
                {
                    if (isEnabled)
                        control.PointerPressed += OnPointerPressed;
                    else
                        control.PointerPressed -= OnPointerPressed;
                }
            });
        }

        public static bool GetIsEnabled(Control element) => element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(Control element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control)
            {
                // 🔥 Переводим фокус на сам элемент (клик по фону)
                // Это автоматически снимет фокус с TextBox
                if (control.Focusable)
                {
                    control.Focus();
                }
                else
                {
                    // Если элемент не фокусируемый, ищем родительское окно
                    var topLevel = TopLevel.GetTopLevel(control);
                    if (topLevel != null)
                    {
                        topLevel.FocusManager?.Focus(null); // или используем другой фокусируемый элемент
                    }
                }
            }
        }
    }
}