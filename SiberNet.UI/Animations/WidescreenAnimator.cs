using System.Windows;
using System.Windows.Media.Animation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using SiberNet.UI.Infrastructure.Base;

namespace SiberNet.UI.Infrastructure.Animators
{
    public class WidescreenAnimator : BaseWindowAnimation, IWidescreenService, IDisposable
    {
        private readonly IEventBus _eventBus;

        private double _storedLeft = 200;
        private double _storedWidth = 900;

        // Внутренний кэш аниматора, чтобы знать своё текущее состояние корпуса
        private bool _isCurrentlyExpanded = false;

        public WidescreenAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Подписываемся на наш ультимативный рекорд с булом
            _eventBus.Subscribe<IWidescreenService.ToggleWidescreen>(OnToggleWidescreenRequested);
        }

        private void OnToggleWidescreenRequested(IWidescreenService.ToggleWidescreen msg)
        {
            if (msg == null) return;

            // Предохранитель: если мы уже стоим в нужной ширине — курим бамбук, видеокарту не трогаем
            if (_isCurrentlyExpanded == msg.IsVisible) return;
            _isCurrentlyExpanded = msg.IsVisible;

            // Дёргаем аппаратный двухступенчатый движок (Сценарий А или Б)
            AnimateWindow(msg.IsVisible);
        }

        private void AnimateWindow(bool expand)
        {
            ExecuteOnUi(window =>
            {
                var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 };
                var stepDuration = TimeSpan.FromSeconds(0.7);

                if (expand)
                {
                    // --- ⚡ СЦЕНАРИЙ А: РАСКРЫТИЕ В WIDESCREEN ---
                    _storedLeft = window.Left;
                    _storedWidth = window.Width;

                    double screenW = SystemParameters.PrimaryScreenWidth;
                    double targetLeft = 100;
                    double targetWidth = screenW - 200;

                    var leftAnim = new DoubleAnimation(window.Left, targetLeft, stepDuration) { EasingFunction = ease };

                    leftAnim.Completed += (s, args) =>
                    {
                        var widthAnim = new DoubleAnimation(window.Width, targetWidth, stepDuration) { EasingFunction = ease };
                        window.BeginAnimation(Window.WidthProperty, widthAnim);
                    };

                    window.BeginAnimation(Window.LeftProperty, leftAnim);
                }
                else
                {
                    // --- 🔄 СЦЕНАРИЙ Б: СХЛОПЫВАНИЕ ОБРАТНО В МЕССЕНДЖЕР ---
                    var widthCollapseAnim = new DoubleAnimation(window.Width, _storedWidth, stepDuration) { EasingFunction = ease };

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
            _eventBus.Unsubscribe<IWidescreenService.ToggleWidescreen>(OnToggleWidescreenRequested);
        }
    }
}
