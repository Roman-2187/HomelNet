using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Interfaces.Repositories;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageService _messageService;

        [ObservableProperty] private Guid _currentUserId;
        [ObservableProperty] private string _currentUserName = string.Empty;
        [ObservableProperty] private bool _isChatOpen = false;
        [ObservableProperty] private string _inputText = string.Empty;
        [ObservableProperty] private string _selectedFriendName = string.Empty;
        [ObservableProperty] private Guid? _selectedFriendId;
        [ObservableProperty] private ObservableCollection<MessageEntity> _messages = new();

        public ChatViewModel(IEventBus eventBus, IMessageService messageService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Subscribe<IContactsVm.FriendSelected>(OnFriendSelected);

            // 🔥 Ловим факт сохранения в SQLite (от репозитория)
            _eventBus.Subscribe<IMessageRepo.Saved>(OnMessageSaved);
        }

        private void OnUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;
            CurrentUserId = msg.User.Id;
            CurrentUserName = msg.User.FirstName ?? "Вы";
        }

        private void OnUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;
            CurrentUserId = msg.User.Id;
            CurrentUserName = msg.User.FirstName ?? "Вы";
        }

        private async void OnFriendSelected(IContactsVm.FriendSelected msg)
        {
            if (msg?.Friend == null) return;

            CurrentUserId = msg.CurrentUserId;
            SelectedFriendId = msg.Friend.Id;
            SelectedFriendName = msg.Friend.FirstName ?? "Собеседник";

            Messages.Clear();
            IsChatOpen = true;

            try
            {
                var history = await _messageService.GetPrivateHistoryAsync(CurrentUserId, SelectedFriendId.Value);
                if (history != null)
                {
                    foreach (var m in history)
                    {
                        m.IsOutgoing = m.SenderId == CurrentUserId;
                    }
                    Messages = new ObservableCollection<MessageEntity>(history);
                }
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ИСТОРИИ]: {ex.Message}"));
            }
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || SelectedFriendId == null) return;

            var rawText = InputText.Trim();
            InputText = string.Empty;

            // 🔥 Отправляем через сервис (он дернет API)
            var verdict = await _messageService.SendPrivateMessageAsync(CurrentUserId, SelectedFriendId.Value, rawText);

            if (!verdict.IsValid)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ЧАТА]: {verdict.ErrorMessage}"));
            }
            // Успех обработается в OnMessageSaved (когда SyncService запишет в SQLite)
        }

        // 🔥 Ловим сохранение в SQLite (от SyncService -> Repository)
        private void OnMessageSaved(IMessageRepo.Saved msg)
        {
            if (msg?.Message == null || SelectedFriendId == null) return;

            // Проверяем, принадлежит ли сообщение текущему открытому диалогу
            if ((msg.Message.SenderId == SelectedFriendId.Value && msg.Message.ReceiverId == CurrentUserId) ||
                (msg.Message.SenderId == CurrentUserId && msg.Message.ReceiverId == SelectedFriendId.Value))
            {
                msg.Message.IsOutgoing = msg.Message.SenderId == CurrentUserId;

                // Добавляем, если еще нет
                if (!Messages.Any(m => m.Id == msg.Message.Id))
                {
                    Messages.Add(msg.Message);
                }
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
            _eventBus.Unsubscribe<IMessageRepo.Saved>(OnMessageSaved);
        }
    }
}