using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using HomeNetCore.Models;
using HomeNetCore.Services;
using HomeNetCore.Data.Repositories; // 🔥 Подключили неймспейс твоего репозитория сообщений!
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class UserDashboardViewModel : FormViewModelBase
    {
        private readonly UserService _userService;
        private readonly MessageRepository _messageRepo; // 🔥 ДОБАВИЛИ ПОЛЕ РЕПОЗИТОРИЯ!

        public ChatViewModel ChatVm { get; }

        [ObservableProperty]
        private UserEntity? _selectedFriend;

        [ObservableProperty] private UserEntity? _currentUser;
        [ObservableProperty] private ObservableCollection<UserEntity> _friends = new();

        // 🔥 ДОБАВИЛИ ТРЕТИЙ ПАРАМЕТР В КОНСТРУКТОР ДЛЯ DI-КОНТЕЙНЕРА
        public UserDashboardViewModel(EventBus eventBus, UserService userService, ChatViewModel chatVm, MessageRepository messageRepo) : base(eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo)); // 🔥 СОХРАНИЛИ

            // 🔥 ПРАВИЛЬНОЕ МЕСТО: Подписываемся на вход СРАЗУ при создании в DI, а не при кликах!
            _eventBus.Subscribe<UserLoggedMessage>(async msg => await OnUserAuthenticatedAsync(msg.User));
            _eventBus.Subscribe<UserRegisteredMessage>(async msg => await OnUserAuthenticatedAsync(msg.User));

            // Передаем выбранного друга во вложенный чат через общую шину данных
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedFriend))
                {
                    if (this.SelectedFriend != null)
                    {
                        // 🔥 ПУЛЯЕМ СОБЫТИЕ В АВТОБУС! Клик долетит до чата в любой точке памяти
                        _eventBus.Publish(new FriendSelectedMessage(this.SelectedFriend));

                        // Включаем флаг анимации полёта чата
                        ChatVm.IsChatOpen = true;
                    }
                    else
                    {
                        ChatVm.IsChatOpen = false;
                    }
                }
            };

            // 🔥 ЛОВИМ ОТПРАВКУ ИЗ ВЛОЖЕННОГО ЧАТА И СОХРАНЯЕМ В БАЗУ ДАННЫХ
            _eventBus.Subscribe<NewMessageSentMessage>(async msg =>
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
                    // 1. Загоняем эсэмэску в нашу умную отказоустойчивую базу данных через внедренный репозиторий
                    await _messageRepo.SaveMessageAsync(entity);

                    // 2. Возвращаемся в UI-поток, чтобы мгновенно отобразить её в ленте чата
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (SelectedFriend != null && SelectedFriend.Id == msg.ReceiverId)
                        {
                            ChatVm.Messages.Add(entity);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ОШИБКА БЭКЕНДА ЧАТА]: Не удалось сохранить СМС: {ex.Message}");
                }
            });
        }

        private async Task OnUserAuthenticatedAsync(UserEntity user)
        {
            if (user == null) return;

            // Даем 1 секунду форме логина, чтобы она красиво улетела вверх
            await Task.Delay(1000);

            try
            {
                // Запрос к базе данных (SQLite или Postgres)
                var allUsers = await _userService.GetAllUsersAsync();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
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

                    // 🔥 МГНОВЕННЫЙ ВЗЛЁТ: Меняем свойство, и стиль UniversalFlyOutBottomStyle выталкивает дашборд!
                    ControlVisibility = Visibility.Visible;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки пользователей: {ex.Message}");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Даже если база упала, открываем пустой дашборд, чтобы приложение не зависло
                    ControlVisibility = Visibility.Visible;
                });
            }
        }
    }
}
