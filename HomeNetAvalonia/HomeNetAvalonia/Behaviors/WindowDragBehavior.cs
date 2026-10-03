using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace HomeNetAvalonia.Behaviors
{
    /// <summary>
    /// Стерильный прикрепляемый перетаскиватель окон мессенджера SiberNet.
    /// </summary>
    public static class WindowDragBehavior
    {
        // Создаем Attached Property (прикрепляемое свойство)
        public static readonly AttachedProperty<bool> EnableDragProperty =
            AvaloniaProperty.RegisterAttached<InputElement, bool>("EnableDrag", typeof(WindowDragBehavior), false);

        static WindowDragBehavior()
        {
            EnableDragProperty.Changed.AddClassHandler<InputElement>((element, args) => OnEnableDragChanged(element, args));
        }

        public static bool GetEnableDrag(InputElement element) => element.GetValue(EnableDragProperty);
        public static void SetEnableDrag(InputElement element, bool value) => element.SetValue(EnableDragProperty, value);

        private static void OnEnableDragChanged(InputElement element, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool newValue)
            {
                if (newValue)
                {
                    element.PointerPressed += Element_PointerPressed;
                }
                else
                {
                    element.PointerPressed -= Element_PointerPressed;
                }
            }
        }

        private static void Element_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Visual visual && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
            {
                // 🔥 ОФИЦИАЛЬНОЕ ИСПРАВЛЕНИЕ ДЛЯ AVALONIA 11+: 
                // Используем GetTopLevel вместо устаревшего GetVisualRoot
                var window = TopLevel.GetTopLevel(visual) as Window;

                // Запускаем родной DragMove встроенными силами Авалонии
                window?.BeginMoveDrag(e);
            }
        }
    }
}
