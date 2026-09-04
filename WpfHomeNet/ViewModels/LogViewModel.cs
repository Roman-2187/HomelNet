using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WpfHomeNet.Messaging;
using WpfHomeNet.UiHelpers;


namespace WpfHomeNet.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged
    {
        #region Поля и переменные
        private readonly LogQueueManager _queueManager;
        private readonly EventBus _eventBus;
        private readonly LogWindow _logWindow;
        #endregion

        #region Свойства
        public double Offset { get; set; } = 1;
        public bool IsVisible => _logWindow.Visibility == Visibility.Visible;
        #endregion

        #region Конструктор

        public LogViewModel(EventBus eventBus, LogQueueManager logQueueManager, LogWindow logWindow)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _queueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));

            _eventBus.Subscribe<WindowPositionChangedMessage>(PositionLogWindow);
            _eventBus.Subscribe<LogWindowVisibilityChangedMessage>(OnVisibilityCommandReceived);

          
        }

        private void OnVisibilityCommandReceived(LogWindowVisibilityChangedMessage msg)
        {
            if (msg.IsVisible)
            {
                _logWindow.Show();

                // Как только админ первый раз нажал кнопку "Показать" — будим наш канал! 🚀
                // Благодаря защите (if (_isReady) return) повторные клики ничего не сломают.
                _queueManager.SetReady();
            }
            else
            {
                _logWindow.Hide();
            }

            
        }


        private void PositionLogWindow(WindowPositionChangedMessage msg)
        {
            if (!msg.IsLoaded) return;

            // Строгая, линейная привязка лога к правому краю главного окна
            _logWindow.Left = msg.Left + msg.Width;
            _logWindow.Top = msg.Top;
            _logWindow.Height = msg.Height;
            _logWindow.Width = 650;

        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}

