using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class DeleteUsersViewModel : FormViewModelBase, IDisposable
    {
        private readonly IDeleteService _srv;
        private readonly ILogger _log;

        [ObservableProperty] private ObservableCollection<UserEntity> _foundUsers = new();
        [ObservableProperty] private UserEntity? _selectedUser;
        [ObservableProperty] private string _targetUserId = string.Empty;

        public ObservableCollection<string> DeletedUsersHistory { get; } = new();

        public DeleteUsersViewModel(IDeleteService srv, IEventBus bus, ILogger log, NavigationStateManager nav)
            : base(bus, nav)
        {
            _srv = srv ?? throw new ArgumentNullException(nameof(srv));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            SubmitButtonText = "Удалить";

            // 🔥 КРАСОТА ДЛЯ ИНСПЕКТОРА: Передаём строго именованные методы! 🧼
            EventBus.Subscribe<IAdminMenuViewModel.DeleteFormRequested>(OnDeleteFormRequested);
            EventBus.Subscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);

            ResetForm(sync: true);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnDeleteFormRequested(IAdminMenuViewModel.DeleteFormRequested msg)
        {
            ResetForm(sync: true);
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
                // 🔥 ЗАПОМИНАЕМ ИМЯ НА ВХОДЕ: Пока бэкенд не начал удаление и кэш не сбросился!
                string? userNameForHistory = SelectedUser != null
                    ? $"{SelectedUser.FirstName} {SelectedUser.LastName}"
                    : null;

                var verdict = await _srv.DeleteUserAsync(TargetUserId, SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {
                    _log.LogInformation($"[DeleteVM] Юзер {verdict.ParsedId} стёрт.");

                    EventBus.Publish(this, new IDeleteUserViewModel.Deleted(verdict.ParsedId ?? -1));

                    // 🔥 ФОРМИРУЕМ СТРОКУ: Если мы запомнили имя, пишем его, иначе — точечно
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
            ResetForm(sync: true);
            EventBus.Publish(this, new IAdminMenuViewModel.DeleteFormCloseRequested());
        }

        public void Dispose()
        {
            // 🔥 ЧИСТАЯ ОТПИСКА ПО ИМЕНАМ МЕТОДОВ
            EventBus.Unsubscribe<IAdminMenuViewModel.DeleteFormRequested>(OnDeleteFormRequested);
            EventBus.Unsubscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);
        }
    }
}
