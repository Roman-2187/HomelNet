using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetAvalonia.Animators
{
    public class CloseWindowAnimator : IDisposable
    {
        private readonly IEventBus _eventBus;

        public CloseWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            // Подписываемся на твой старый ивент закрытия из Ядра
            _eventBus.Subscribe<IMainViewModel.CloseRequest>(OnWindowCloseRequested);
        }

        private void OnWindowCloseRequested(IMainViewModel.CloseRequest msg)
        {
            // Переходим в UI-поток Авалонии
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    if (window == null) return;

                    // 1. Активируем триггер анимации в XAML, просто добавив класс к окну!
                    window.Classes.Add("exit-anim");

                    // 2. Ждем ровно 500мс (время анимации) и тушим приложение
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        desktop.Shutdown();
                    };
                    timer.Start();
                }
            });
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IMainViewModel.CloseRequest>(OnWindowCloseRequested);
        }
    }
}
