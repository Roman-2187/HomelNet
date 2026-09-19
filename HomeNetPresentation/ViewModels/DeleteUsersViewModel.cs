using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; 
using HomeNetCore.Models;
using HomeNetServices.Services.Identity;

namespace HomeNetPresentation.ViewModels
{
    // 🔥 ВЕРНУЛИ РОДНОЕ ИМЯ КЛАССА! Больше никакого скрещивания с админкой!
    public partial class DeleteUsersViewModel : FormViewModelBase
    {
        #region Поля и свойства 🦾
        private readonly DeleteService _deleteService;
        private readonly ILogger _logger;

        [ObservableProperty] private UserEntity? _selectedUser;
        [ObservableProperty] private string _targetUserId = string.Empty;
        [ObservableProperty] private bool _canDelete;
        #endregion

        #region Конструктор
        public DeleteUsersViewModel(DeleteService deleteService, IEventBus eventBus, ILogger logger) : base(eventBus)
        {
            _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            IsControlVisible = false;
            SubmitButtonText = "Удалить";
            StatusMessage = "Введите ID ";

            InitEventBus();
        }
        #endregion

        #region Хитрые хуки (Нано-реакция на изменение полей) ⚙️
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

        partial void OnCanDeleteChanged(bool value) => DeleteCommand.NotifyCanExecuteChanged();
        #endregion

        #region Нано-команды 🧼

        private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(TargetUserId);

        [RelayCommand(CanExecute = nameof(CanExecuteSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск пользователя в базе данных...";
            CanDelete = false;
            var (isSuccess, message, _) = await _deleteService.SearchUserAsync(TargetUserId);
            StatusMessage = message;
            CanDelete = isSuccess;

            _logger.LogInformation($"Выполнен поиск пользователя. Результат: {message}");
        }

        private bool CanExecuteDelete() => CanDelete;

        [RelayCommand(CanExecute = nameof(CanExecuteDelete))]
        private async Task DeleteAsync()
        {
            int id = SelectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);
            if (id == -1) return;

            StatusMessage = $"Удаление пользователя с ID {id}...";
            var (isSuccess, message) = await _deleteService.DeleteUserAsync(id);
            StatusMessage = message;

            if (isSuccess)
            {
                // 🔥 ПОПРАВИЛИ: Публикуем короткий ивент удаления юзера
                EventBus.Publish(this, new IDeleteUserViewModel.Deleted(id));
                _logger.LogInformation($"Пользователь с ID {id} успешно удален.");
                ResetForm();
            }
            else
            {
                _logger.LogWarning($"Не удалось удалить пользователя с ID {id} : {message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            IsControlVisible = false;
        }
        #endregion

        #region Вспомогательная логика
        private void InitEventBus()
        {
            // 🔥 ПОПРАВИЛИ: Перешли на короткий ивент базового класса форм
            EventBus.Publish(this, new IFormViewModelBase.VisibilityChanged(GetType(), false));

            EventBus.Subscribe<IFormViewModelBase.VisibilityChanged>(msg =>
            {
                if (msg.FormType != GetType() && msg.IsVisible)
                {
                    ResetForm();
                    IsControlVisible = false;
                }
            });
        }

        private void ResetForm()
        {
            TargetUserId = string.Empty;
            StatusMessage = "Введите ID ";
            CanDelete = false;
            SelectedUser = null;
        }
        #endregion
    }
}
