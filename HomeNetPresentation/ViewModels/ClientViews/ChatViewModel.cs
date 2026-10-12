using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Тонкая витрина визуального окна чата SiberNet.
    /// Больше вообще не знает про существование репозиториев БД. Рулится сервисом! 🧼
    /// </summary>
    public partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageService _messageService; // Внедряем сервис вместо репозитория!

        private Guid _currentUserId;

        [ObservableProperty] private string _currentUserName = string.Empty;
        [ObservableProperty] private bool _isChatOpen = false;
        [ObservableProperty] private string _inputText = string.Empty;
        [ObservableProperty] private Guid? _selectedFriendId;
        [ObservableProperty] private ObservableCollection<MessageEntity> _messages = new();

        public ChatViewModel(IEventBus eventBus, IMessageService messageService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Subscribe<IContactsVm.FriendSelected>(OnFriendSelected);
        }

        private void OnUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;
            _currentUserId = msg.User.Id;
            CurrentUserName = msg.User.FirstName!;
        }

        private void OnUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;
            _currentUserId = msg.User.Id;
            CurrentUserName = msg.User.FirstName!;
        }

        private void OnFriendSelected(IContactsVm.FriendSelected msg)
        {
            if (msg?.Friend == null) return;

            SelectedFriendId = msg.Friend.Id;
            Messages.Clear();
            IsChatOpen = true;

            // Логику первоначальной подгрузки истории через навигатор мы подключим на следующем шаге
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || SelectedFriendId == null) return;

            var rawText = InputText.Trim();
            InputText = string.Empty; // Сразу очищаем поле ввода в киберпанк-стиле для отзывчивости UI

            // 🚀 СТАНДАРТ РЕГИСТРАЦИИ: Спрашиваем вердикт у сервиса чата!
            var verdict = await _messageService.SendPrivateMessageAsync(_currentUserId, SelectedFriendId.Value, rawText);

            if (verdict.IsValid && verdict.CreatedMessage != null)
            {
                // Если сервис успешно всё сохранил — добавляем СМС на экран
                Messages.Add(verdict.CreatedMessage);

               
            }
            else
            {
                // Если произошёл сбой — выводим ошибку в статусбар
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ЧАТА]: {verdict.ErrorMessage}"));
            }
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(InputText) && SelectedFriendId != null;

        partial void OnInputTextChanged(string value) => SendMessageCommand.NotifyCanExecuteChanged();
        partial void OnSelectedFriendIdChanged(Guid? value) => SendMessageCommand.NotifyCanExecuteChanged();


        public void Dispose()
        {
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Unsubscribe<IContactsVm.FriendSelected>(OnFriendSelected);
        }
    }
}
