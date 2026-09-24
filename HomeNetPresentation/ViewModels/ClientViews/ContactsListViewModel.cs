using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; // Наш интерфейс из Ядра
using HomeNetCore.Models;
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

            // Ловим сигналы входа, чтобы загрузить контакты (прямо как раньше в дашборде)
            _eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(async msg => await LoadContactsAsync(msg.User));
            _eventBus.Subscribe<IUsersTableViewModel.Added>(async msg => await LoadContactsAsync(msg.User));
        }

        /// <summary>
        /// Реактивный хук тулкита: кликнули по другу в списке
        /// </summary>
        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value != null)
            {
                // 🔥 Публикуем короткий ивент из Ядра! ChatViewModel его мгновенно поймает
                _eventBus.Publish(this, new IContactsListViewModel.FriendSelected(value));
            }
        }

        private async Task LoadContactsAsync(UserEntity? currentUser)
        {
            if (currentUser == null) return;

            // Даем форме логина 1 секунду красиво улететь
            await Task.Delay(1000);

            try
            {
                var allUsers = await _userService.GetAllAsync();
                Friends.Clear();

                if (allUsers != null)
                {
                    foreach (var u in allUsers)
                    {
                        if (u.Id != currentUser.Id)
                        {
                            if (u.FirstName == null) u.FirstName = "Пользователь без имени";
                            Friends.Add(u);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Если база упала, сообщаем в статус-бар
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Контакты СБОЙ]: {ex.Message}"));
            }
        }
    }
}
