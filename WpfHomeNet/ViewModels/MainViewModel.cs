using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace WpfHomeNet.ViewModels
{

    // ViewModel для главного окна
    public class MainViewModel : INotifyPropertyChanged, IStatusUpdater
    {
       
        private readonly UserService _userService; 
        private readonly ILogger _logger; 

        // Событие для уведомления об изменении свойств
        public event PropertyChangedEventHandler? PropertyChanged;

        // Свойства для хранения данных
        private ObservableCollection<UserEntity> _users = []; // Коллекция пользователей

        private Visibility _scrollViewerVisibility = Visibility.Collapsed; // Видимость ScrollViewer

        private string _statusText = string.Empty; // Текстовое сообщение статуса

        
        public MainViewModel(UserService userService, ILogger logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Свойства с уведомлениями об изменении
        public ObservableCollection<UserEntity> Users
        {
            get => _users;
            private set
            {
                if (_users == value) return;
                _users = value;
                OnPropertyChanged(nameof(Users)); 
            }
        }

        public Visibility ScrollViewerVisibility
        {
            get => _scrollViewerVisibility;
            set
            {
                _scrollViewerVisibility = value;
                OnPropertyChanged(nameof(ScrollViewerVisibility));
            }
        } 
        
        
        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (_statusText == value) return;
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }


        // Методы управления видимостью ScrollViewer
        public void ShowScrollViewer() => ScrollViewerVisibility = Visibility.Visible;
        public void HideScrollViewer() => ScrollViewerVisibility = Visibility.Collapsed;



       

        // Методы для работы со статусом
        public void SetStatus(string message) => StatusText = message;


       

        // Метод уведомления об изменении свойства
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }




        // Основной метод загрузки пользователей
        public async Task LoadUsersAsync()
        {
            StatusText = "Загрузка..."; // Устанавливаем статус загрузки

            try
            {
                var users = await _userService.GetAllUsersAsync(); // Получаем пользователей

                if (users == null)
                {
                    HandleError("Получены пустые данные");
                    return;
                }

                Users.Clear(); // Очищаем старую коллекцию
                Users = new ObservableCollection<UserEntity>(users); // Заполняем новую

                var userCount = users.Count;

                if (userCount == 0)
                {
                    HandleSuccess($"Список пользователей пуст");
                }
                else
                {
                    HandleSuccess($"Загружено {userCount} пользователей");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Ошибка загрузки: {ex.Message}");
            }
        }



        // Вспомогательные методы для обработки результатов
        private void HandleSuccess(string message)
        {
            _logger?.LogInformation(message);
            StatusText = message;
        }

        private void HandleError(string message)
        {
            _logger?.LogError(message);
            StatusText = message;
        }
    }


}
