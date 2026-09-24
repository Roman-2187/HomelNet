using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class DeleteUsersViewModel : FormViewModelBase
    {
        #region Поля и свойства 🦾
        private readonly IUserService _userService; // 🔥 Добавили сервис пользователей для выгрузки всего списка
        private readonly IDeleteService _deleteService;
        private readonly ILogger _logger;

        public ObservableCollection<UserEntity> FoundUsers { get; private set; } = new();

        [ObservableProperty] private UserEntity? _selectedUser;
        [ObservableProperty] private string _targetUserId = string.Empty;
        [ObservableProperty] private bool _canDelete;
        #endregion

        #region Конструктор
        public DeleteUsersViewModel(IDeleteService deleteService, IUserService userService, IEventBus eventBus, ILogger logger, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubmitButtonText = "Удалить";

            // 🎧 Ловим пинок из командного пункта админки
            EventBus.Subscribe<IAdminMenuViewModel.DeleteFormRequested>(async msg =>
            {
                ResetForm();
                await LoadAllUsersAsync(); // 🔥 При каждом открытии формы сразу выгружаем всех из SQLite!
            });

            ResetForm();
            _ = LoadAllUsersAsync(); // Первичная выгрузка при инициализации
        }
        #endregion

        #region Вспомогательная асинхронная выгрузка
        private async Task LoadAllUsersAsync()
        {
            StatusMessage = "Загрузка списка пользователей из базы данных HomeNet...";
            try
            {
                var allUsers = await _userService.GetAllAsync();
                FoundUsers.Clear();

                if (allUsers != null)
                {
                    foreach (var user in allUsers)
                    {
                        FoundUsers.Add(user);
                    }
                    StatusMessage = $"Всего пользователей в базе: {FoundUsers.Count}. Выберите юзера для удаления.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки пользователей: {ex.Message}";
            }
        }
        #endregion

        #region Хитрые хуки ⚙️
        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null)
            {
                TargetUserId = value.Id.ToString();
                CanDelete = true;
            }
        }

        partial void OnTargetUserIdChanged(string value)
        {
            SearchCommand.NotifyCanExecuteChanged();
            CanDelete = int.TryParse(value, out int parsedId) && parsedId > 0 || (!string.IsNullOrWhiteSpace(value) && CanDelete);
        }

        partial void OnCanDeleteChanged(bool value) => ExecuteDeleteCommand.NotifyCanExecuteChanged();
        #endregion

        #region Нано-команды 🧼

        private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(TargetUserId);

        [RelayCommand(CanExecute = nameof(CanExecuteSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск пользователя...";
            CanDelete = false;
            FoundUsers.Clear();

            var (isSuccess, message, foundUser) = await _deleteService.SearchUserAsync(TargetUserId);
            StatusMessage = message;
            CanDelete = isSuccess;

            if (isSuccess && foundUser != null)
            {
                FoundUsers.Add(foundUser);
                SelectedUser = foundUser;
            }

            _logger.LogInformation($"Выполнен точечный поиск пользователя. Результат: {message}");
        }

        private bool CanExecuteDelete() => CanDelete;

        [RelayCommand(CanExecute = nameof(CanExecuteDelete))]
        private async Task ExecuteDelete()
        {
            int id = SelectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);
            if (id == -1) return;

            StatusMessage = $"Удаление пользователя с ID {id}...";
            var (isSuccess, message) = await _deleteService.DeleteUserAsync(id);
            StatusMessage = message;

            if (isSuccess)
            {
                EventBus.Publish(this, new IDeleteUserViewModel.Deleted(id));
                _logger.LogInformation($"Пользователь с ID {id} успешно удален.");

                // Перезагружаем список, чтобы удаленный юзер сразу исчез с экрана
                await LoadAllUsersAsync();
                TargetUserId = string.Empty;
                CanDelete = false;
                SelectedUser = null;
            }
            else
            {
                _logger.LogWarning($"Не удалось удалить пользователя с ID {id} : {message}");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            ResetForm();
            await LoadAllUsersAsync();

            // 🔥 КРИЧИМ В АВТОБУС: Командный пункт, гаси этот экран!
            EventBus.Publish(this, new IAdminMenuViewModel.DeleteFormCloseRequested());
        }


       

        #endregion

        #region Вспомогательная логика
        private void ResetForm()
        {
            TargetUserId = string.Empty;
            StatusMessage = "Введите ID или выберите пользователя ниже ";
            CanDelete = false;
            SelectedUser = null;
            FoundUsers.Clear();
        }
        #endregion
    }
}
