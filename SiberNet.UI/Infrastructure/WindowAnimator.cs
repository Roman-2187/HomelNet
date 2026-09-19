using System;
using System.Windows;
using System.Windows.Media.Animation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; // 🔥 Где лежат наши новые интерфейсы

namespace SiberNet.UI.Infrastructure
{
    public class WindowAnimator : IDisposable
    {
        private readonly IEventBus _eventBus;

        // Память для обычного развертывания (🔳)
        private bool _isMaximized = false;
        private double _storedWidth = 900;
        private double _storedHeight = 600;

        // Память для трансформации дебага (⚡)
        private double _storedLeftBeforeDebug = 200;
        private double _storedWidthBeforeDebug = 900;

        public WindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 🔌 Коммутируем новые лаконичные кабели в шину!
            _eventBus.Subscribe<IMainViewModel.CloseRequest>(OnWindowCloseRequested);
            _eventBus.Subscribe<IMainViewModel.ToggleSize>(OnToggleWindowSizeRequested);
            _eventBus.Subscribe<IAdminMenuViewModel.ToggleAnimation>(OnToggleGlobalLoggerRequested);
        }

        // 🎬 1. КАРИКАТУРНОЕ ПАДЕНИЕ ВНИЗ (Крестик)
        private void OnWindowCloseRequested(IMainViewModel.CloseRequest msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.MainWindow;
                if (window == null) return;

                double screenHeight = SystemParameters.PrimaryScreenHeight;
                var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

                var exitAnimation = new DoubleAnimation
                {
                    To = screenHeight + 150,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = easeIn
                };

                exitAnimation.Completed += (s, args) => Application.Current.Shutdown();
                window.BeginAnimation(Window.TopProperty, exitAnimation);
            });
        }

        // 🎬 2. ПЛАВНОЕ ВЫРАСТАНИЕ ИЗ ЦЕНТРА ВО ВСЕ СТОРОНЫ (Кнопка 🔳)
        private void OnToggleWindowSizeRequested(IMainViewModel.ToggleSize msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.MainWindow;
                if (window == null) return;

                double screenW = SystemParameters.PrimaryScreenWidth;
                double screenH = SystemParameters.PrimaryScreenHeight;
                double targetWidth, targetHeight, targetTop, targetLeft;

                if (!_isMaximized)
                {
                    _storedWidth = window.Width;
                    _storedHeight = window.Height;

                    targetWidth = screenW;
                    targetHeight = screenH;
                    targetTop = 0;
                    targetLeft = 0;
                    _isMaximized = true;
                }
                else
                {
                    targetWidth = _storedWidth;
                    targetHeight = _storedHeight;
                    targetTop = (screenH - targetHeight) / 2;
                    targetLeft = (screenW - targetWidth) / 2;
                    _isMaximized = false;
                }

                var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 };
                var duration = TimeSpan.FromSeconds(0.85);

                window.BeginAnimation(Window.WidthProperty, new DoubleAnimation(window.Width, targetWidth, duration) { EasingFunction = ease });
                window.BeginAnimation(Window.HeightProperty, new DoubleAnimation(window.Height, targetHeight, duration) { EasingFunction = ease });
                window.BeginAnimation(Window.TopProperty, new DoubleAnimation(window.Top, targetTop, duration) { EasingFunction = ease });
                window.BeginAnimation(Window.LeftProperty, new DoubleAnimation(window.Left, targetLeft, duration) { EasingFunction = ease });
            });
        }

        // 🎬 3. КИБЕРПАНК-ТРАНСФОРМАЦИЯ: ОТПОЛЗАНИЕ ВЛЕВО И РОСТ ВПРАВО (Кнопка ⚡)
        private void OnToggleGlobalLoggerRequested(IAdminMenuViewModel.ToggleAnimation msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.MainWindow;
                if (window == null) return;

                double targetWidth, targetLeft;
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var duration = TimeSpan.FromSeconds(0.6);

                // 🔥 Обрати внимание: теперь вытаскиваем флаг из вложенной MainViewModel в рекорде
                if (msg.MainViewModel.IsGlobalLoggerVisible)
                {
                    _storedLeftBeforeDebug = window.Left;
                    _storedWidthBeforeDebug = window.Width;

                    targetLeft = 40;
                    targetWidth = _storedWidthBeforeDebug + 400;
                }
                else
                {
                    targetLeft = _storedLeftBeforeDebug;
                    targetWidth = _storedWidthBeforeDebug;
                }

                window.BeginAnimation(Window.LeftProperty, new DoubleAnimation(window.Left, targetLeft, duration) { EasingFunction = ease });
                window.BeginAnimation(Window.WidthProperty, new DoubleAnimation(window.Width, targetWidth, duration) { EasingFunction = ease });
            });
        }

        public void Dispose()
        {
            // Разрываем новые контакты при уничтожении аниматора
            _eventBus.Unsubscribe<IMainViewModel.CloseRequest>(OnWindowCloseRequested);
            _eventBus.Unsubscribe<IMainViewModel.ToggleSize>(OnToggleWindowSizeRequested);
            _eventBus.Unsubscribe<IAdminMenuViewModel.ToggleAnimation>(OnToggleGlobalLoggerRequested);
        }
    }
}
