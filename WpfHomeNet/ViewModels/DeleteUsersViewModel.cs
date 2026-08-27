using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    // Наследуемся от FormViewModelBase, чтобы была доступна магия ControlVisibility
    public class DeleteUsersViewModel : FormViewModelBase
    {
        private readonly UserService _userService;
        private MainViewModel? _mainViewModel;

        // Обязательно public! С точным соблюдением регистра букв!
        private ObservableCollection<UserEntity>? _mainUsersList;
        public ObservableCollection<UserEntity>? MainUsersList
        {
            get => _mainUsersList;
            set
            {
                _mainUsersList = value;
                OnPropertyChanged(nameof(MainUsersList)); // Пинаем XAML, чтобы он перерисовал список!
            }
        }


        private UserEntity? _selectedUser;
        public UserEntity? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));

                // Если админ выбрал юзера из списка — автоматом подставляем ID и включаем кнопку!
                if (_selectedUser != null)
                {
                    TargetUserId = _selectedUser.Id.ToString();
                    CanDelete = true;
                }
            }
        }




        // 1. НАСТОЯЩЕЕ СВОЙСТВО ДЛЯ ТЕКСТБОКСА ИЗ ТВОЕЙ РАЗМЕТКИ!
        private string _targetUserId = string.Empty;
        public string TargetUserId
        {
            get => _targetUserId;
            set
            {
                // Используем SetField из FormViewModelBase для оповещения XAML
                if (SetField(ref _targetUserId, value))
                {
                    // Активируем кнопку "Найти" только если поле не пустое
                    ((RelayCommand)SearchCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // 2. СВОЙСТВО ДЛЯ АКТИВАЦИИ КНОПКИ "УДАЛИТЬ"
        private bool _canDelete;
        public bool CanDelete
        {
            get => _canDelete;
            set => SetField(ref _canDelete, value);
        }

        public ICommand SearchCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }
        public Action<int>? OnUserDeletedFromDb { get; internal set; }

        public DeleteUsersViewModel(UserService userService)
        {

            StatusMessage = "Введите ID ";

            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            ControlVisibility = Visibility.Collapsed;

            // Команда поиска по базе данных
            SearchCommand = new RelayCommand(
                execute: async (obj) => await ExecuteSearchCommandAsync(),
                canExecute: (param) => !string.IsNullOrWhiteSpace(TargetUserId)
                
            );

            // Команда удаления из базы данных
            DeleteCommand = new RelayCommand(
                execute: async (obj) => await ExecuteDeleteCommandAsync(),
                canExecute: (param) => CanDelete
            );

            // Команда отмены (схлопывание формы)
            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetForm();
                    ControlVisibility = Visibility.Collapsed;
                }
            );
        }

        public void ConnectToMainViewModel(MainViewModel mainVm) => _mainViewModel = mainVm;

        private void ResetForm()
        {
            TargetUserId = string.Empty;
            StatusMessage = "Введите ID ";
            CanDelete = false;
        }

        // Логика поиска через UserService
        private async Task ExecuteSearchCommandAsync()
        {
            StatusMessage = "Поиск пользователя в базе данных...";
            CanDelete = false;

            if (!int.TryParse(TargetUserId, out int id))
            {
                StatusMessage = "Ошибка: ID должен состоять только из цифр!";
                return;
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user != null)
                {
                    StatusMessage = $"Найден: {user.FirstName} {user.LastName} ({user.Email})";
                    CanDelete = true; // Зажигаем кнопку "Удалить"
                }
                else
                {
                    StatusMessage = $"Пользователь с ID {id} не существует.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка SQLite: {ex.Message}";
            }
        }

        // Логика удаления
        private async Task ExecuteDeleteCommandAsync()
        {
            // Берем ID либо из выделенного юзера, либо из текстового поля (если вбили руками)
            int id = _selectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);

            if (id == -1) return;

            StatusMessage = $"Удаление пользователя с ID {id}...";

            try
            {
                // 1. Стираем из базы данных
                await _userService.DeleteUserAsync(id);

                StatusMessage = $"Пользователь с ID {id} успешно удален из системы.";
                CanDelete = false;

                // 2. Дергаем наш безотказный делегат-провод, чтобы обновить Главный экран и статус-бар!
                OnUserDeletedFromDb?.Invoke(id);

                // 3. Сбрасываем выделение, чтобы очистить твой модный ListBox
                SelectedUser = null;
                TargetUserId = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка удаления: {ex.Message}";
            }
        }

    }
}




