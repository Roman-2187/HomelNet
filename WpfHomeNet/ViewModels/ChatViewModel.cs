using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using HomeNetCore.Data.Interfaces; // Твой интерфейс логгера
using HomeNetCore.Models;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly EventBus _eventBus;
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
        public ChatViewModel(EventBus eventBus, ILogger logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation($"[ChatVM] Конструктор запущен. Хэш-код экземпляра: {this.GetHashCode()}");

            // 🔥 Магия синхронизации через автобус данных
            _eventBus.Subscribe<FriendSelectedMessage>(msg =>
            {
                if (msg.Friend == null) return;

                // 📝 Записываем в логи прилет посылки
                _logger.LogDebug($"[ChatVM] Шина EventBus доставила FriendSelectedMessage! Прилетел: {msg.Friend.FirstName} (ID: {msg.Friend.Id})");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Записываем друга в свойство, и тулкит сам вызовет OnSelectedFriendChanged
                    SelectedFriend = msg.Friend;
                });
            });
        }

        // 🔥 Магия тулкита: этот метод автоматически вызывается при ЛЮБОМ изменении свойства SelectedFriend
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
            _eventBus.Publish(new NewMessageSentMessage(textToSend, SelectedFriend.Id));

            await Task.CompletedTask;
        }

        [RelayCommand]
        private void AttachFile()
        {
            _logger.LogDebug("[ChatVM] Нажата кнопка прикрепления файла (скрепка).");
        }
    }
}
