using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces;             // Контракты репозиториев и IEventBus из Ядра 🧼 UFO
using HomeNetCore.Messaging;              // Наши обновленные чистые рекорды-сигналы из Ядра
using HomeNetCore.Models;
using HomeNetServices.Services.Identity;
using HomeNetServices.Services.Messaging;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class UserDashboardViewModel : FormViewModelBase
    {
        private readonly UserService _userService;
        private readonly IMessageRepository _messageRepo;

        public ChatViewModel ChatVm { get; }

        [ObservableProperty] private UserEntity? _selectedFriend;
        [ObservableProperty] private UserEntity? _currentUser;

        // Оставляем ObservableCollection — фреймворки (WPF/Avalonia) ее одинаково обожают! 🤝
        [ObservableProperty] private ObservableCollection<UserEntity> _friends = new();

        // 🔥 ИСПРАВЛЕНО: Конструктор принимает чистый интерфейс IEventBus из Ядра и прокидывает в базу через base
        public UserDashboardViewModel(IEventBus eventBus, UserService userService, ChatViewModel chatVm, IMessageRepository messageRepo) : base(eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));

            // 🔥 ИСПРАВЛЕНО: Обращаемся к базовому свойству EventBus с БОЛЬШОЙ буквы!
            EventBus.Subscribe<UserLoggedMessage>(async msg => await OnUserAuthenticatedAsync(msg.User));

            // 🔥 ИСПРАВЛЕНО: Заменили старый UserRegisteredMessage на наш системный UserAddedMessage из Ядра! 🧼
            EventBus.Subscribe<UserAddedMessage>(async msg => await OnUserAuthenticatedAsync(msg.User));

            // ЛОВИМ ОТПРАВКУ ИЗ ВЛОЖЕННОГО ЧАТА И СОХРАНЯЕМ В БАЗУ ДАННЫХ
            EventBus.Subscribe<NewMessageSentMessage>(async msg =>
            {
                if (CurrentUser == null || SelectedFriend == null) return;

                var entity = new MessageEntity
                {
                    SenderId = CurrentUser.Id,
                    ReceiverId = msg.ReceiverId,
                    Text = msg.Text,
                    MediaType = "Text",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    // 1. Загоняем эсэмэску в нашу базу данных через внедренный репозиторий
                    await _messageRepo.SaveMessageAsync(entity);

                    // 2. Мгновенно отображаем в ленте чата (без жестких диспетчеров WPF) 🧼
                    if (SelectedFriend != null && SelectedFriend.Id == msg.ReceiverId)
                    {
                        ChatVm.Messages.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    // Сигнализируем в статус-бар о системной ошибке бэкенда через EventBus с большой буквы
                    EventBus.Publish(this, new StatusTextChangedMessage($"[БЭКЕНД ЧАТА СБОЙ]: {ex.Message}"));
                }
            });
        }

        /// <summary>
        /// Перехватчик Toolkit: автоматически срабатывает при клике на друга в списке 🎯
        /// </summary>
        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value != null)
            {
                // 🔥 ИСПРАВЛЕНО: ПУЛЯЕМ СОБЫТИЕ В АВТОБУС С БОЛЬШОЙ БУКВЫ! 
                EventBus.Publish(this, new FriendSelectedMessage(value));

                // Включаем флаг анимации полёта чата
                ChatVm.IsChatOpen = true;
            }
            else
            {
                ChatVm.IsChatOpen = false;
            }
        }

        private async Task OnUserAuthenticatedAsync(UserEntity user)
        {
            if (user == null) return;

            // Даем 1 секунду форме логина, чтобы она красиво улетела вверх
            await Task.Delay(1000);

            try
            {
                // Асинхронный запрос к базе данных (SQLite или Postgres)
                var allUsers = await _userService.GetAllUsersAsync();

                CurrentUser = user;
                Friends.Clear();

                if (allUsers != null)
                {
                    foreach (var u in allUsers)
                    {
                        if (u.Id != CurrentUser.Id)
                        {
                            if (u.FirstName == null)
                            {
                                u.FirstName = "Пользователь без имени";
                            }
                            Friends.Add(u);
                        }
                    }
                }

                // Включаем видимость через наш новый булевый флаг из FormViewModelBase! 🧼✨
                IsControlVisible = true;
            }
            catch (Exception ex)
            {
                // Перевели на БОЛЬШУЮ БУКВУ
                EventBus.Publish(this, new StatusTextChangedMessage($"Ошибка загрузки пользователей: {ex.Message}"));

                // Даже если база упала, открываем пустой дашборд, чтобы приложение не зависло
                IsControlVisible = true;
            }
        }
    }
}
