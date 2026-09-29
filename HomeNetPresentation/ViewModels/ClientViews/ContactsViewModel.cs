using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Реактивный список контактов SiberNet.
    /// Полностью отвязан от СУБД, сидит на подсосе данных от таблицы пользователей.
    /// </summary>
    public partial class ContactsViewModel : ObservableObject, IContactsVm, IDisposable
    {
        private readonly IEventBus _eventBus;

        [ObservableProperty] private bool _isMenuOpen; // Локальный бул для шторки мессенджера
        private UserEntity? _currentUser; // Храним текущего, чтобы знать кого отсекать

        [ObservableProperty] private UserEntity? _selectedFriend;
        [ObservableProperty] private ObservableCollection<UserEntity> _friends = new();

        public ContactsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed); // 🔥 ГЛАВНЫЙ ПОДСОС
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Чистая реактивность) 🧼

        private void OnUsersRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg?.Users == null) return;

            // 🔥 Твой фирменный проход пачкой прямо из прилетевшего события таблицы!
            var filtered = msg.Users
                .Where(u => _currentUser == null || u.Id != _currentUser.Id)
                .Select(u => {
                    if (u.FirstName == null) u.FirstName = "Пользователь без имени";
                    return u;
                });

            Friends = new ObservableCollection<UserEntity>(filtered);
        }



        [RelayCommand]
        private void Logout()
        {
            IsMenuOpen = false; // 🔥 Тушим шторку пользователя при выходе!
            _eventBus.Publish(this, new IUserVm.LogoutClient());
        }

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg?.User == null) return;

            if (_currentUser != null && msg.User.Id == _currentUser.Id) return;

            if (msg.User.FirstName == null)
                msg.User.FirstName = "Пользователь без имени";

            Friends.Add(msg.User);
        }

        private void OnUserDeleted(IDeleteUserVm.Deleted msg)
        {
            if (msg == null) return;

            var friendToRemove = Friends.FirstOrDefault(f => f.Id == msg.Id);
            if (friendToRemove != null)
            {
                Friends.Remove(friendToRemove);
            }
        }

        #endregion

        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value != null)
            {
                _eventBus.Publish(this, new IContactsVm.FriendSelected(value));
            }
        }

        /// <summary>
        /// Вызывается системой при логине/смене пользователя, чтобы отсечь себя из списка контактов
        /// </summary>
        public void SetCurrentUser(UserEntity? currentUser)
        {
            _currentUser = currentUser;
            // Просим таблицу выплюнуть стейт повторно, чтобы пересчитать список без себя
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public void Dispose()
        {
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
        }

        #endregion
    }
}
