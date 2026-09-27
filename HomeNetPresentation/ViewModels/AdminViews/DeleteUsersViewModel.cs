using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation; // Тянем энумы под новые условия
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Полностью автономная форма удаления пользователей SiberNet.
    /// Синхронизируется исключительно по фактам изменения вкладок из автобуса.
    /// </summary>
    public partial class DeleteUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IDeleteService _srv;
        private readonly ILogger _log;

        [ObservableProperty] private ObservableCollection<UserEntity> _foundUsers = new();
        [ObservableProperty] private UserEntity? _selectedUser;
        [ObservableProperty] private string _targetUserId = string.Empty;

        public ObservableCollection<string> DeletedUsersHistory { get; } = new();

        public DeleteUsersViewModel(IDeleteService srv, IEventBus bus, ILogger log, AdminNavigationManager nav)
            : base(bus, nav)
        {
            _srv = srv ?? throw new ArgumentNullException(nameof(srv));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            SubmitButtonText = "Удалить";

            // 🔥 ПОДПИСКИ ПО ФАКТАМ: Ловим изменение вкладок и факт успешного стирания из бэка
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);

            ResetForm(sync: false); // При старте не греем базу заранее
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            // 🔥 ЕСЛИ НАВИГАТОР ПОДТВЕРДИЛ: Открыта вкладка удаления — только тогда шуршим в базу за списком!
            if (msg.ActiveTab == AdminSubTab.DeleteUsers)
            {
                ResetForm(sync: true);
            }
        }

        private void OnUserDeleted(IDeleteUserVm.Deleted msg)
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
            var users = await _srv.GetAllUsersAsync();
            FoundUsers = new ObservableCollection<UserEntity>(users ?? Enumerable.Empty<UserEntity>());
            StatusMessage = FoundUsers.Any() ?
                $"Всего в базе: {FoundUsers.Count}. Выберите юзера." : "В базе пока нет пользователей.";

            SearchCommand.NotifyCanExecuteChanged();
        }

        private void ResetForm(bool sync = false)
        {
            TargetUserId = string.Empty;
            SelectedUser = null;
            StatusMessage = "Введите ID или выберите пользователя";
            if (sync) _ = SyncUsersAsync();

            SearchCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null && TargetUserId != value.Id.ToString())
                TargetUserId = value.Id.ToString();

            StatusMessage = value != null
                ? $"Выбран: ID {value.Id} — {value.FirstName} {value.LastName}"
                : $"Всего в базе: {FoundUsers.Count}. Выберите юзера.";

            SearchCommand.NotifyCanExecuteChanged();
        }

        partial void OnTargetUserIdChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedUser = null;
                SearchCommand.NotifyCanExecuteChanged();
                return;
            }

            var match = FoundUsers.FirstOrDefault(u => u.Id.ToString() == value.Trim());
            SelectedUser = match;

            if (match == null)
                StatusMessage = $"Введён сторонний ID: {value}. Нажмите 'Поиск'.";

            SearchCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск...";
            var verdict = await _srv.SearchUserAsync(TargetUserId);
            StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

            if (verdict.IsValid && verdict.FoundUser != null)
            {
                FoundUsers.Add(verdict.FoundUser);
                SelectedUser = verdict.FoundUser;
            }
        }

        private bool CanSearch()
        {
            if (FoundUsers.Count == 0 && string.IsNullOrWhiteSpace(TargetUserId))
                return false;

            if (SelectedUser == null)
                return true;

            return SelectedUser.Id.ToString() != TargetUserId.Trim();
        }

        [RelayCommand]
        private async Task ExecuteDeleteAsync()
        {
            try
            {
                string? userNameForHistory = SelectedUser != null
                    ? $"{SelectedUser.FirstName} {SelectedUser.LastName}"
                    : null;

                var verdict = await _srv.DeleteUserAsync(TargetUserId, SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {
                    _log.LogInformation($"[DeleteVM] Юзер {verdict.ParsedId} стёрт.");

                    _eventBus.Publish(this, new IDeleteUserVm.Deleted(verdict.ParsedId ?? -1));

                    string userInfo = userNameForHistory != null
                        ? $"{userNameForHistory} (ID {verdict.ParsedId})"
                        : $"ID {verdict.ParsedId} (Точечно)";

                    DeletedUsersHistory.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Удален: {userInfo}");

                    await Task.Delay(500);
                    ResetForm(sync: true);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            ResetForm(sync: false);
            // 🔥 ЧИСТОТА: Просто шлём OpenDelete повторно, навигатор-тумблер сам закроет эту панель в None!
            _eventBus.Publish(this, new IAdminVm.OpenDelete());
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
        }
    }
}
