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
    /// Системный пуленепробиваемый обработчик сворачивания окна Зайбернета. 🧼
    /// </summary>
    public class MinimizeWindowAnimator : IDisposable
    {
        private readonly IEventBus _eventBus;

        public MinimizeWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<IMainViewModel.MinimizeRequest>(OnWindowMinimizeRequested);
        }

        private void OnWindowMinimizeRequested(IMainViewModel.MinimizeRequest msg)
        {
            // Переходим в UI-поток Авалонии, чтобы безопасно дернуть окно ОС
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    if (window == null) return;

                    // Классическое моментальное сворачивание силами операционной системы
                    window.WindowState = WindowState.Minimized;
                }
            });
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IMainViewModel.MinimizeRequest>(OnWindowMinimizeRequested);
        }
    }
}
