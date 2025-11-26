using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace WpfHomeNet.ViewModels
{

    public class MainViewModel : INotifyPropertyChanged, IStatusUpdater
    {
        private readonly UserService _userService;
        private readonly ILogger _logger;


        public MainViewModel(UserService userService, ILogger logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private ObservableCollection<UserEntity> _users = new ObservableCollection<UserEntity>();
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


        private UserEntity? _selectedUser;
        public UserEntity? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                CanDelete = value != null;
                OnPropertyChanged(nameof(CanDelete));
            }
        }

        public bool CanDelete { get; private set; }

        private Visibility _scrollViewerVisibility = Visibility.Collapsed;
        public Visibility ScrollViewerVisibility
        {
            get => _scrollViewerVisibility;
            set
            {
                _scrollViewerVisibility = value;
                OnPropertyChanged(nameof(ScrollViewerVisibility));
            }
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (_statusText == value) return;
                _statusText = value;
                Debug.WriteLine($"SETTER: {_statusText} → {value}");
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public void SetStatus(string message) => StatusText = message;

        public void ShowScrollViewer()
        {
            ScrollViewerVisibility = Visibility.Visible;
        }

        public async Task LoadUsersAsync()
        {
            StatusText = "Загрузка...";

            try
            {
                var users = await _userService.GetAllUsersAsync();

                if (users == null)
                {
                    HandleError("Получены пустые данные");
                    return;
                }

                Users.Clear();
                Users = new ObservableCollection<UserEntity>(users);

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






        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }





}
