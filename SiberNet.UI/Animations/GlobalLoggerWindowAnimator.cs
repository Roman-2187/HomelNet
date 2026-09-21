using System;
using System.Windows;
using System.Windows.Media.Animation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using SiberNet.UI.Infrastructure.Base;

namespace SiberNet.UI.Infrastructure.Animators
{
    /// <summary>
    /// Киберпанк-аниматор раздвижения окна для выезда Глобального Терминала Логов.
    /// Работает по двухступенчатой схеме: 0.7 сек сдвиг влево + 0.7 сек раскрытие шторки.
    /// </summary>
    public class GlobalLoggerWindowAnimator : BaseWindowAnimation
    {
        private readonly IEventBus _eventBus;

        // Бэкап координат для возврата окна в исходное состояние
        private double _storedLeft = 200;
        private double _storedWidth = 900;

        public GlobalLoggerWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Слушаем триггер анимации оверлея логов
            _eventBus.Subscribe<ITitleBarViewModel.ToggleAnimation>(OnToggleGlobalLoggerRequested);
        }

        private void OnToggleGlobalLoggerRequested(ITitleBarViewModel.ToggleAnimation msg)
        {
            ExecuteOnUi(window =>
            {
                // Наш фирменный плавный экспоненциальный сглаживатель движения
                var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 };
                var stepDuration = TimeSpan.FromSeconds(0.7); // 🔥 Жестко 0.7 секунды на каждый этап

                if (msg.IsVisible)
                {
                    // --- ⚡ СЦЕНАРИЙ А: РАСКРЫТИЕ ГЛОБАЛЬНОГО ЛОГГЕРА ---

                    // 1. Запоминаем текущие размеры окна перед трансформацией
                    _storedLeft = window.Left;
                    _storedWidth = window.Width;

                    // Вычисляем целевые размеры на основе экрана
                    double screenW = SystemParameters.PrimaryScreenWidth;
                    double targetLeft = 100;            // Едем налево до сотки от края
                    double targetWidth = screenW - 200; // Растягиваем ширину до 100px от правого края

                    // 🎬 ШАГ 1: Погнали всем корпусом влево (0.7 сек)
                    var leftAnim = new DoubleAnimation(window.Left, targetLeft, stepDuration) { EasingFunction = ease };

                    // 🎬 ШАГ 2: Как только приехали на X=100 — запускаем рост ширины вправо (еще 0.7 сек)
                    leftAnim.Completed += (s, args) =>
                    {
                        var widthAnim = new DoubleAnimation(window.Width, targetWidth, stepDuration) { EasingFunction = ease };
                        window.BeginAnimation(Window.WidthProperty, widthAnim);
                    };

                    // Стартуем первый шаг
                    window.BeginAnimation(Window.LeftProperty, leftAnim);
                }
                else
                {
                    // --- 🔄 СЦЕНАРИЙ Б: СХЛОПЫВАНИЕ ОБРАТНО В МЕССЕНДЖЕР ---
                    // Сначала за 0.7 сек убираем шторку (схлопываем Width обратно в 900px)
                    var widthCollapseAnim = new DoubleAnimation(window.Width, _storedWidth, stepDuration) { EasingFunction = ease };

                    // Как только ширина вернулась — плавно возвращаем всё окно на исходную координату Left (0.7 сек)
                    widthCollapseAnim.Completed += (s, args) =>
                    {
                        var leftReturnAnim = new DoubleAnimation(window.Left, _storedLeft, stepDuration) { EasingFunction = ease };
                        window.BeginAnimation(Window.LeftProperty, leftReturnAnim);
                    };

                    window.BeginAnimation(Window.WidthProperty, widthCollapseAnim);
                }
            });
        }

        public override void Dispose()
        {
            _eventBus.Unsubscribe<ITitleBarViewModel.ToggleAnimation>(OnToggleGlobalLoggerRequested);
        }
    }
}
