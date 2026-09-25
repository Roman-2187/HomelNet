using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    // 🔥 Реализуем IDisposable для полной зачистки памяти от подписок
    public partial class DeleteUsersViewModel : FormViewModelBase, IDisposable
    {
        private readonly IDeleteService _deleteService;
        private readonly ILogger _logger;

        [ObservableProperty] private ObservableCollection<UserEntity> _foundUsers = new();
        public ObservableCollection<string> DeletedUsersHistory { get; } = new();

        [ObservableProperty] private UserEntity? _selectedUser;
        [ObservableProperty] private string _targetUserId = string.Empty;

        public DeleteUsersViewModel(IDeleteService deleteService,
            IEventBus eventBus, ILogger logger, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubmitButtonText = "Удалить";

            // 🔥 ЧИСТОТА: Передаем именованные методы вместо анонимных лямбд! 🧼
            EventBus.Subscribe<IAdminMenuViewModel.DeleteFormRequested>(OnDeleteFormRequested);
            EventBus.Subscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);

            ResetForm();
            _ = SyncUsersAsync();
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private async void OnDeleteFormRequested(IAdminMenuViewModel.DeleteFormRequested msg)
        {
            ResetForm();
            await SyncUsersAsync();
        }

        private void OnUserDeleted(IDeleteUserViewModel.Deleted msg)
        {
            var userToRemove = FoundUsers.FirstOrDefault(u => u.Id == msg.Id);
            if (userToRemove != null)
            {
                FoundUsers.Remove(userToRemove);
            }
        }

        #endregion

        private async Task SyncUsersAsync()
        {
            StatusMessage = "Загрузка списка пользователей...";
            try
            {
                var users = await _deleteService.GetAllUsersAsync();

                FoundUsers = new ObservableCollection<UserEntity>(users ?? Enumerable.Empty<UserEntity>());

                StatusMessage = FoundUsers.Count > 0
                    ? $"Всего пользователей в базе: {FoundUsers.Count}. Выберите юзера для удаления."
                    : "В базе данных HomeNet пока нет пользователей.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
            }
        }

        public void ResetForm()
        {
            TargetUserId = string.Empty;
            SelectedUser = null;
            StatusMessage = "Введите ID или выберите пользователя ниже";
            FoundUsers.Clear();
        }

        #region 🎯 ХУК СВЯЗИ
        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null)
            {
                TargetUserId = value.Id.ToString();
            }
        }
        #endregion

        #region 🦾 НАНО-КОМАНДЫ ДЛЯ БЭКЕНДА

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetUserId)) return;

            StatusMessage = "Поиск пользователя...";
            FoundUsers.Clear();

            var verdict = await _deleteService.SearchUserAsync(TargetUserId);
            StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

            if (verdict.IsValid && verdict.FoundUser != null)
            {
                FoundUsers.Add(verdict.FoundUser);
                SelectedUser = verdict.FoundUser;
            }
        }

        [RelayCommand]
        private async Task ExecuteDeleteAsync()
        {
            StatusMessage = string.Empty;
            try
            {
                var verdict = await _deleteService.DeleteUserAsync(TargetUserId, SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {
                    _logger.LogInformation($"[DeleteVM] Пользователь с ID {verdict.ParsedId} успешно стёрт.");
                    EventBus.Publish(this, new IDeleteUserViewModel.Deleted(verdict.ParsedId ?? -1));

                    string userInfo = SelectedUser != null
                        ? $"ID {verdict.ParsedId}: {SelectedUser.FirstName} {SelectedUser.LastName}"
                        : $"ID {verdict.ParsedId} (Точечное удаление)";
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    DeletedUsersHistory.Add($"[{timestamp}] ❌ Удален {userInfo}");

                    await Task.Delay(500);
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При удалении произошла ошибка: {ex.Message}";
                _logger.LogError($"[DeleteVM] Критический сбой удаления: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            ResetForm();
            await SyncUsersAsync();
            EventBus.Publish(this, new IAdminMenuViewModel.DeleteFormCloseRequested());
        }
        #endregion

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        /// <summary>
        /// Полностью отписывает методы от шины событий при уничтожении вью-модели.
        /// </summary>
        public void Dispose()
        {
            EventBus.Unsubscribe<IAdminMenuViewModel.DeleteFormRequested>(OnDeleteFormRequested);
            EventBus.Unsubscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);
        }

        #endregion
    }
}