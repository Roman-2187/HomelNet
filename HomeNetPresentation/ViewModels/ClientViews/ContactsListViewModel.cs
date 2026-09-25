using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetServices.Routing;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class ContactsListViewModel : ObservableObject, IContactsListViewModel
    {
        private readonly IEventBus _eventBus;
        private readonly IUserService _userService;

        [ObservableProperty] private UserEntity? _selectedFriend;
        [ObservableProperty] private ObservableCollection<UserEntity> _friends = new();

        public ContactsListViewModel(IEventBus eventBus, IUserService userService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            _eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(async msg => await LoadContactsAsync(msg.User));

            _eventBus.Subscribe<IUsersTableViewModel.Added>(msg =>
            {
                if (msg.User == null) return;
                if (msg.User.FirstName == null) msg.User.FirstName = "Пользователь без имени";
                Friends.Add(msg.User);
            });

            _eventBus.Subscribe<IDeleteUserViewModel.Deleted>(msg =>
            {
                // 🔥 СОСТЫКОВКА: Поменяли msg.UserId на правильный msg.Id
                var friendToRemove = Friends.FirstOrDefault(f => f.Id == msg.Id);
                if (friendToRemove != null)
                {
                    Friends.Remove(friendToRemove);
                }
            });
        }

        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value != null)
            {
                _eventBus.Publish(this, new IContactsListViewModel.FriendSelected(value));
            }
        }

        private async Task LoadContactsAsync(UserEntity? currentUser)
        {
            if (currentUser == null) return;
            await Task.Delay(1000);

            try
            {
                var allUsers = await _userService.GetAllAsync();

                // 🔥 ЧИСТЫЙ LINQ: Отсекаем текущего юзера и мапим пустые имена за один проход пачкой!
                Friends = new ObservableCollection<UserEntity>(
                    allUsers
                        .Where(u => u.Id != currentUser.Id)
                        .Select(u => {
                            if (u.FirstName == null) u.FirstName = "Пользователь без имени";
                            return u;
                        })
                );
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Контакты СБОЙ]: {ex.Message}"));
            }
        }
    }
}
