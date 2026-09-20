using System;
using System.Windows;

namespace SiberNet.UI.Infrastructure.Base
{
    /// <summary>
    /// Базовый фундамент для всех киберпанк-анимаций окон.
    /// Автоматически берет на себя безопасную работу с Dispatcher и валидацию окна.
    /// </summary>
    public abstract class BaseWindowAnimation : IDisposable
    {
        public abstract void Dispose();

        /// <summary>
        /// Безопасно выполняет анимацию в главном потоке WPF/Avalonia.
        /// </summary>
        protected void ExecuteOnUi(Action<Window> animationAction)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var window = Application.Current?.MainWindow;
                if (window != null)
                {
                    animationAction(window);
                }
            });
        }
    }
}
