using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using System.Collections.ObjectModel;

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

        public ContactsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // Слушаем изменения таблицы пользователей в БД
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);

            // 🔥 ЛОВИМ ГЛОБАЛЬНЫЕ ИВЕНТЫ АВТОРИЗАЦИИ НАПРЯМУЮ ИЗ АВТОБУСА
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnGlobalUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnGlobalUserSignedUp);
        }




        // Переводим отслеживание на ID вошедшего пользователя
        private Guid? _currentUserId;

        private void OnGlobalUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            // 🎯 Забираем ID юзера прямо из прилетевшего сообщения входа
            _currentUserId = msg.User.Id;
            _currentUser = msg.User;

            // Запрашиваем обновление списка у БД
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        private void OnGlobalUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;

            // 🎯 Забираем ID юзера прямо из прилетевшего сообщения регистрации
            _currentUserId = msg.User.Id;
            _currentUser = msg.User;

            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        private void OnUsersRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg?.Users == null) return;

            // 🎯 Железобетонная фильтрация: исключаем по сохраненному из автобуса ID.
            // Теперь Иван гарантированно отсекается, даже если запросы в шине перемешались!
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
            _eventBus.Publish(this, new IUserVm.LogoutClient());
        }

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg?.User == null) return;
            if (_currentUser != null && msg.User.Id == _currentUser.Id) return;

            if (msg.User.FirstName == null) msg.User.FirstName = "Пользователь без имени";
            Friends.Add(msg.User);
        }

        private void OnUserDeleted(IDeleteUserVm.Deleted msg)
        {
            if (msg == null) return;
            var friendToRemove = Friends.FirstOrDefault(f => f.Id == msg.Id);
            if (friendToRemove != null) Friends.Remove(friendToRemove);
        }

        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value != null)
            {
                // 🔥 Просто швыряем намерение в автобус, навигатор чата сам поймает этот факт
                _eventBus.Publish(this, new IContactsVm.FriendSelected(value));
            }
        }

        public void SetCurrentUser(UserEntity? currentUser)
        {
            _currentUser = currentUser;
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeleted);

            // 🔥 Очищаем подписки на авторизацию при уничтожении вью-модели
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnGlobalUserSignedIn);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnGlobalUserSignedUp);
        }
    }
}
