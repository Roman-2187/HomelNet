using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; 
using HomeNetCore.Models;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        // Текущий собеседник, чат с которым открыт
        [ObservableProperty] private UserEntity? _selectedFriend;

        // Коллекция реальных объектов сообщений из базы данных
        [ObservableProperty] private ObservableCollection<MessageEntity> _messages = new();

        // Поле ввода сообщения, привязанное к TextBox в XAML
        [ObservableProperty] private string _messageText = string.Empty;

        // Свойство для управления анимацией чата
        [ObservableProperty] private bool _isChatOpen = false;

        // 🔥 КЛАССИЧЕСКИЙ КОНСТРУКТОР: один, чистый и понятный DI-контейнеру
        public ChatViewModel(IEventBus eventBus, ILogger logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation($"[ChatVM] Конструктор запущен. Хэш-код экземпляра: {this.GetHashCode()}");

            // 🔥 Магия синхронизации через автобус данных! Слушаем левую панель контактов 🧼⚡
            _eventBus.Subscribe<IContactsListViewModel.FriendSelected>(msg =>
            {
                if (msg.Friend == null) return;

                _logger.LogDebug($"[ChatVM] Шина EventBus доставила FriendSelected! Прилетел: {msg.Friend.FirstName} (ID: {msg.Friend.Id})");

                // Записываем друга в свойство, и тулкит сам вызовет OnSelectedFriendChanged. 
                SelectedFriend = msg.Friend;
            });
        }

        // Автоматически вызывается при ЛЮБОМ изменении свойства SelectedFriend
        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            _logger.LogInformation($"[ChatVM] Свойство SelectedFriend ИЗМЕНЕНО! Собеседник: {value?.FirstName ?? "NULL"} (ID: {value?.Id ?? 0}). Хэш-код: {this.GetHashCode()}");
        }

        // Команда отправки эсэмэски
        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || SelectedFriend == null) return;

            string textToSend = MessageText.Trim();
            MessageText = string.Empty;

            _logger.LogInformation($"[ChatVM] Отправка сообщения. Кому ID: {SelectedFriend.Id}, Текст: {textToSend}");

            // 🔥 ПОПРАВИЛИ: Публикуем чистый, укороченный по хозяину рекорд-сообщение в воздух
            _eventBus.Publish(this, new IChatViewModel.NewSent(textToSend, SelectedFriend.Id));

            await Task.CompletedTask;
        }

        [RelayCommand]
        private void AttachFile()
        {
            _logger.LogDebug("[ChatVM] Нажата кнопка прикрепления файла (скрепка).");
        }
    }
}
