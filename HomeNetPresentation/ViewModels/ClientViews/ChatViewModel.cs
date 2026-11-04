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
    /// <summary>
    /// Автономная киберпанк-витрина чата SiberNet.
    /// Благодаря сквозному UI-конвейеру работает на полной скорости без единого лока! 🧼
    /// </summary>
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
            _eventBus.Subscribe<IMessageRepo.SaveRequested>(OnMessageReceivedInRealTime);
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

            // 🧼 Полная безопасность: Мы зашли из UI-потока, локи больше не нужны
            Messages.Clear();
            IsChatOpen = true;

            try
            {
                var history = await _messageService.GetPrivateHistoryAsync(CurrentUserId, SelectedFriendId.Value);

                if (history != null)
                {
                    var historyList = history.ToList();

                    foreach (var m in historyList)
                    {
                        m.IsOutgoing = m.SenderId == CurrentUserId;
                    }

                    Messages = new ObservableCollection<MessageEntity>(historyList);
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

            var verdict = await _messageService.SendPrivateMessageAsync(CurrentUserId, SelectedFriendId.Value, rawText);

            if (verdict.IsValid && verdict.CreatedMessage != null)
            {
                verdict.CreatedMessage.IsOutgoing = true;

                // Локальное эхо выполняется атомарно в родном UI-потоке
                if (!Messages.Any(m => m.Id == verdict.CreatedMessage.Id))
                {
                    Messages.Add(verdict.CreatedMessage);
                }
            }
            else
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ЧАТА]: {verdict.ErrorMessage}"));
            }
        }

        private void OnMessageReceivedInRealTime(IMessageRepo.SaveRequested msg)
        {
            if (msg?.Message == null || SelectedFriendId == null) return;

            // Проверяем, принадлежит ли прилетевшее сообщение текущему открытому диалогу
            if ((msg.Message.SenderId == SelectedFriendId.Value && msg.Message.ReceiverId == CurrentUserId) ||
                (msg.Message.SenderId == CurrentUserId && msg.Message.ReceiverId == SelectedFriendId.Value))
            {
                msg.Message.IsOutgoing = msg.Message.SenderId == CurrentUserId;

             
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
            _eventBus.Unsubscribe<IMessageRepo.SaveRequested>(OnMessageReceivedInRealTime);
        }
    }
}
