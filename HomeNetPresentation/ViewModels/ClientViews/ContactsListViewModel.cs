using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class ContactsListViewModel : ObservableObject, IContactsListViewModel, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IUserService _userService;

        [ObservableProperty] private UserEntity? _selectedFriend;
        [ObservableProperty] private ObservableCollection<UserEntity> _friends = new();

        public ContactsListViewModel(IEventBus eventBus, IUserService userService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ЧИСТОТА: Никаких лямбд, шпион в Инспекторе теперь видит всё! 🧼
           
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

       

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg?.User == null) return;

            if (msg.User.FirstName == null)
                msg.User.FirstName = "Пользователь без имени";

            Friends.Add(msg.User);
        }

        private void OnUserDeleted(IDeleteUserVm.Deleted msg)
        {
            if (msg == null) return;

            // Находим и выкидываем удалённого юзера из нашего списка контактов
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
                _eventBus.Publish(this, new IContactsListViewModel.FriendSelected(value));
            }
        }

        private async Task LoadContactsAsync(UserEntity? currentUser)
        {
            if (currentUser == null) return;
            await Task.Delay(1000); // Твоя фирменная пауза 🎬

            try
            {
                var allUsers = await _userService.GetAllAsync();

                // 🔥 Твой крутой проход пачкой: отсекаем себя и мапим пустые имена
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

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public void Dispose()
        {
            
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
        }

        #endregion
    }
}
