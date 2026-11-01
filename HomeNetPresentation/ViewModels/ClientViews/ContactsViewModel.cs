using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Interfaces.Sinc; // 🔥 Подключаем неймспейс ОРМ-событий
using HomeNetCore.Models.Entity;
using System.Collections.ObjectModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Реактивный список контактов SiberNet.
    /// Полностью отвязан от СУБД и логики навигации чата! 🧼
    /// </summary>
    public partial class ContactsViewModel : ObservableObject, IContactsVm, IDisposable
    {
        private readonly IEventBus _eventBus;

        [ObservableProperty] private bool _isMenuOpen;
        private UserEntity? _currentUser;

        [ObservableProperty] private UserEntity? _selectedFriend;
        [ObservableProperty] private ObservableCollection<UserEntity> _friends = new();

        [ObservableProperty] private string _currentUserName = "Авторизация...";

        private Guid? _currentUserId;

        public ContactsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // Слушаем изменения таблицы пользователей в БД через центральный репозиторий
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);

            // 🔥 ПЕРЕВОДИМ НА СИСТЕМНЫЕ СОБЫТИЯ СЛОЯ ДАННЫХ
            _eventBus.Subscribe<ISyncCoordinator.UserInserted>(OnUserAdded);
            _eventBus.Subscribe<ISyncCoordinator.UserDeleted>(OnUserDeleted);

            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnGlobalUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnGlobalUserSignedUp);
        }

        private void OnGlobalUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id;
            _currentUser = msg.User;
            CurrentUserName = msg.User.FirstName ?? "Вы";

            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        private void OnGlobalUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id;
            _currentUser = msg.User;
            CurrentUserName = msg.User.FirstName ?? "Вы";

            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        private void OnUsersRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg?.Users == null) return;

            var filtered = msg.Users
                .Where(u => _currentUserId == null || u.Id != _currentUserId.Value)
                .Select(u => {
                    if (u.FirstName == null) u.FirstName = "Пользователь без имени";
                    return u;
                });

            Friends = new ObservableCollection<UserEntity>(filtered);
        }

        [RelayCommand]
        private void Logout()
        {
            IsMenuOpen = false;
            CurrentUserName = "Авторизация...";
            _eventBus.Publish(this, new IUserVm.LogoutClient());
        }

        private void OnUserAdded(ISyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null) return;

            // 1. Не добавляем самого себя
            if (_currentUser != null && msg.User.Id == _currentUser.Id) return;

            // 2. 🔥 ЗАЩИТА ОТ ДВОЙНИКОВ: если такой юзер уже есть в списке, не добавляем его снова!
            if (Friends.Any(f => f.Id == msg.User.Id)) return;

            if (msg.User.FirstName == null) msg.User.FirstName = "Пользователь без имени";

            Friends.Add(msg.User);
        }


        // 🔥 Метод теперь принимает системный тип события от репозитория
        private void OnUserDeleted(ISyncCoordinator.UserDeleted msg)
        {
            if (msg == null) return;
            var friendToRemove = Friends.FirstOrDefault(f => f.Id == msg.UserId);
            if (friendToRemove != null) Friends.Remove(friendToRemove);
        }

        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value != null)
            {
                _eventBus.Publish(this, new IContactsVm.FriendSelected(value, _currentUserId ?? Guid.Empty));
            }
        }

        public void SetCurrentUser(UserEntity? currentUser)
        {
            _currentUser = currentUser;
            if (_currentUser != null)
            {
                CurrentUserName = _currentUser.FirstName ?? "Вы";
            }
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);

            // 🔥 Не забываем отписаться от новых интерфейсов в Dispose
            _eventBus.Unsubscribe<ISyncCoordinator.UserInserted>(OnUserAdded);
            _eventBus.Unsubscribe<ISyncCoordinator.UserDeleted>(OnUserDeleted);

            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnGlobalUserSignedIn);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnGlobalUserSignedUp);
        }
    }
}
