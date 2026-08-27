using HomeNetCore.Services;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    // Наследуемся от FormViewModelBase, чтобы была доступна магия ControlVisibility
    public class DeleteUsersViewModel : FormViewModelBase
    {
        private readonly UserService _userService;
        private MainViewModel? _mainViewModel;

       

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
            if (!int.TryParse(TargetUserId, out int id)) return;

            StatusMessage = $"Удаление пользователя с ID {id}...";

            try
            {
                // Вызываем твой метод из сервиса (передаём только ID)
                await _userService.DeleteUserAsync(id);

                StatusMessage = $"Пользователь с ID {id} успешно удален из системы.";
                CanDelete = false;

                // Пинаем таблицу на главном экране через экшн, чтобы строка пропала
                _mainViewModel?.RemoveUserAction?.Invoke(id);

                TargetUserId = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка удаления: {ex.Message}";
            }
        }
    }
}




