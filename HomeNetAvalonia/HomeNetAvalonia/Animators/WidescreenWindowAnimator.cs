using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.MainZone;

namespace HomeNetAvalonia.Animators
{
    /// <summary>
    /// Системный обработчик разворачивания корпуса Зайбернета на весь экран. 🧼
    /// </summary>
    public class WidescreenWindowAnimator : IDisposable
    {
        private readonly IEventBus _eventBus;

        public WidescreenWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Подписываемся на команду изменения размера окна
            _eventBus.Subscribe<IMainViewModel.ToggleSize>(OnWindowToggleSizeRequested);
        }

        private void OnWindowToggleSizeRequested(IMainViewModel.ToggleSize msg)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    if (window == null) return;

                    // Простое переключение состояний: если развернуто — сжимаем, если нормальное — на весь экран
                    if (window.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        window.WindowState = WindowState.Maximized;
                    }
                }
            });
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IMainViewModel.ToggleSize>(OnWindowToggleSizeRequested);
        }
    }
}
