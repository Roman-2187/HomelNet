using System;
using System.Windows;
using System.Windows.Media.Animation;
using HomeNetCore.Interfaces;
using HomeNetCore.Events;

namespace SiberNet.UI.Infrastructure
{
    public class WindowAnimator : IDisposable
    {
        private readonly IEventBus _eventBus;

        public WindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Ловим пинок от ВьюМодели на закрытие!
            _eventBus.Subscribe<RequestWindowCloseMessage>(OnWindowCloseRequested);
        }

        private void OnWindowCloseRequested(RequestWindowCloseMessage msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                double screenHeight = SystemParameters.PrimaryScreenHeight;
                var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

                // 🎬 АНИМАЦИЯ ПАДЕНИЯ
                var exitAnimation = new DoubleAnimation
                {
                    To = screenHeight + 150,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = easeIn
                };

                // Жесткое системное выключение после падения окна
                exitAnimation.Completed += (s, args) =>
                {
                    Application.Current.Shutdown();
                };

                mainWindow.BeginAnimation(Window.TopProperty, exitAnimation);
            });
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<RequestWindowCloseMessage>(OnWindowCloseRequested);
        }
    }
}
