using System;
using Avalonia;
using Avalonia.Controls;

namespace HomeNetAvalonia.Behaviors
{
    /// <summary>
    /// Стерильный прикрепляемый автоскролл для ScrollViewer мессенджера SiberNet.
    /// Автоматически прокручивает чат до самого низа при добавлении новых сообщений.
    /// </summary>
    public static class AutoScrollBehavior
    {
        // Создаем прикрепляемое свойство EnableAutoScroll
        public static readonly AttachedProperty<bool> EnableAutoScrollProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("EnableAutoScroll", typeof(AutoScrollBehavior), false);

        static AutoScrollBehavior()
        {
            // Регистрируем глобальный перехватчик изменений этого свойства для ScrollViewer
            EnableAutoScrollProperty.Changed.AddClassHandler<ScrollViewer>((scrollViewer, args) => OnEnableAutoScrollChanged(scrollViewer, args));
        }

        public static bool GetEnableAutoScroll(ScrollViewer element) => element.GetValue(EnableAutoScrollProperty);
        public static void SetEnableAutoScroll(ScrollViewer element, bool value) => element.SetValue(EnableAutoScrollProperty, value);

        private static void OnEnableAutoScrollChanged(ScrollViewer scrollViewer, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool newValue)
            {
                if (newValue)
                {
                    // 🔥 В Avalonia вместо изменения размера панели мы ловим ивент изменения прокрутки самого ScrollViewer
                    scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                }
                else
                {
                    scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
                }
            }
        }

        private static void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Если размер контента по вертикали изменился (прилетело новое сообщение)
                if (e.ExtentDelta.Y != 0)
                {
                    // Мёртвый автоскролл до упора вниз встроенным методом Авалонии
                    scrollViewer.ScrollToEnd();
                }
            }
        }
    }
}
