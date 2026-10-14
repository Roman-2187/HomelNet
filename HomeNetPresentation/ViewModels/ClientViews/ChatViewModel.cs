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
    /// Автономная киберпанк-витрина чата SiberNet.
    /// Работает строго через сервис сообщений и шину событий! 🧼
    /// </summary>
    public partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageService _messageService;

      

        [ObservableProperty] private Guid _currentUserId; // Было приватным полем, станет свойством!


        [ObservableProperty] private string _currentUserName = string.Empty;
        [ObservableProperty] private bool _isChatOpen = false;
        [ObservableProperty] private string _inputText = string.Empty;

        // Добавляем хранение имени выбранного друга для шапки чата
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

            // 🔥 ОЧЕНЬ ВАЖНО: Слушаем шину на предмет новых входящих сообщений в реальном времени!
           
            _eventBus.Subscribe<IMessageService.MessageReceived>(OnMessageReceivedInRealTime);
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

            // Запоминаем ID текущего юзера и выбранного друга
            CurrentUserId = msg.CurrentUserId;
            SelectedFriendId = msg.Friend.Id;
            SelectedFriendName = msg.Friend.FirstName ?? "Собеседник";
            Messages.Clear();
            IsChatOpen = true;

            try
            {
                // Вытаскиваем сырые сообщения из базы
                var history = await _messageService.GetPrivateHistoryAsync(CurrentUserId, SelectedFriendId.Value);

                // Очищаем старый чат
                Messages.Clear();

                if (history != null)
                {
                    var historyList = history.ToList();

                    foreach (var m in historyList)
                    {
                        m.IsOutgoing = m.SenderId == CurrentUserId;
                        Messages.Add(m); // 🎯 Вызов Add заставит UI-поток Avalonia перерисовать конкретный элемент с правильным Tag!
                    }


                    // Только ТЕПЕРЬ, когда вся история размечена, отдаем ее в UI
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
            InputText = string.Empty; // Мгновенно чистим поле ввода в киберпанк-стиле

            // Отправляем запрос в сервис
            var verdict = await _messageService.SendPrivateMessageAsync(CurrentUserId, SelectedFriendId.Value, rawText);

          

            if (verdict.IsValid && verdict.CreatedMessage != null)
            {
                // Новое сообщение, которое отправил ТЫ, всегда является исходящим!
                verdict.CreatedMessage.IsOutgoing = true;
                Messages.Add(verdict.CreatedMessage);
            }


            else
            {
                // Выводим ошибку в неоновый статусбар приложения
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ЧАТА]: {verdict.ErrorMessage}"));
            }
        }

        private void OnMessageReceivedInRealTime(IMessageService.MessageReceived msg)
        {
            if (msg?.Message == null) return;

            // 🔥 ПРОВЕРКА СВЯЗИ: Если новое сообщение пришло от того друга, чей чат сейчас ОТКРЫТ, 
            // и предназначалось именно МНЕ — мгновенно рендерим его на экране
            if (msg.Message.SenderId == SelectedFriendId && msg.Message.ReceiverId == CurrentUserId)
            {
                // Avalonia сама обновит ListBox/ItemsControl благодаря ObservableCollection
                Messages.Add(msg.Message);
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
            _eventBus.Unsubscribe<IMessageService.MessageReceived>(OnMessageReceivedInRealTime);
        }
    }
}
