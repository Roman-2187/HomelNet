using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces;          
using HomeNetCore.Messaging;             
using HomeNetCore.Models;
using HomeNetServices.Services.Messaging;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IEventBus _eventBus; // Перевели на чистый интерфейс Ядра! 🧼🛸
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

            // 🔥 Магия синхронизации через автобус данных (БЕЗ ДИСПЕТЧЕРОВ WPF!) 🧼⚡
            _eventBus.Subscribe<FriendSelectedMessage>(msg =>
            {
                if (msg.Friend == null) return;

                _logger.LogDebug($"[ChatVM] Шина EventBus доставила FriendSelectedMessage! Прилетел: {msg.Friend.FirstName} (ID: {msg.Friend.Id})");

                // Записываем друга в свойство, и тулкит сам вызовет OnSelectedFriendChanged. 
                // Возврат в UI-поток кроссплатформенно обеспечит SynchronizationContext шины!
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

            // Публикуем чистый рекорд-сообщение в воздух
            _eventBus.Publish(this, new NewMessageSentMessage(textToSend, SelectedFriend.Id));

            await Task.CompletedTask;
        }

        [RelayCommand]
        private void AttachFile()
        {
            _logger.LogDebug("[ChatVM] Нажата кнопка прикрепления файла (скрепка).");
        }
    }
}
