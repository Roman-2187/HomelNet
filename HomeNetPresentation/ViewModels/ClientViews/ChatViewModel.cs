using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Тонкая витрина визуального окна чата SiberNet.
    /// Не знает про существование БД, просто швыряет намерения ввода в автобус.
    /// </summary>
    public partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;

        [ObservableProperty] private string _currentUserName = string.Empty; // 🔥 Свойство для XAML

        // 🔥 КЭШ UI: Храним ID текущего авторизованного пользователя
        private Guid _currentUserId;

        [ObservableProperty] private bool _isChatOpen = false;
        [ObservableProperty] private string _inputText = string.Empty;
        [ObservableProperty] private Guid? _selectedFriendId;

        // 🔥 РЕАКТИВНОСТЬ: Сюда макро-панель (UserViewModel) будет напрямую докидывать сообщения из базы
        [ObservableProperty] private ObservableCollection<MessageEntity> _messages = new();

        public ChatViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Ловим системные веб-ивенты, чтобы чат знал, КТО сейчас сидит за компом
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Subscribe<IContactsVm.FriendSelected>(OnFriendSelected);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Локальный UI-кэш для ID) 🧼




        private void OnUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id; // GUID встанет сюда как родной

            // Раз валидатор на регистрации не пускает пустые имена — берем напрямую!
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
            Messages.Clear(); // Чистим экран перед загрузкой истории нового друга
            IsChatOpen = true;
        }

        #endregion

        #region 🚀 НАНО-КОМАНДЫ UI (Чистый выстрел намерения) 🧼

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText) || SelectedFriendId == null) return;

            // 🔥 ТОТАЛЬНАЯ ЧИСТОТА: Просто швыряем структуру-приказ в автобус!
            // Никаких дат, никаких MessageEntity. Сервис сам всё поймает и запишет.
            _eventBus.Publish(this, new IChatViewModel.Send(
                SenderId: _currentUserId,
                Text: InputText.Trim(),
                ChatType: "Private",
                TargetId: SelectedFriendId
            ));

            // Локальный UI-триггер (если макро-панели нужно подмигнуть анимацией до ответа базы)
            _eventBus.Publish(this, new IChatViewModel.NewSent(InputText.Trim(), SelectedFriendId.Value));

            InputText = string.Empty; // Моментально очищаем поле ввода в киберпанк-стиле
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(InputText) && SelectedFriendId != null;
        }

        // Автоматически пересчитываем доступность кнопки отправки при вводе букв
        partial void OnInputTextChanged(string value) => SendMessageCommand.NotifyCanExecuteChanged();
        partial void OnSelectedFriendIdChanged(Guid? value) => SendMessageCommand.NotifyCanExecuteChanged();

        #endregion

        public void Dispose()
        {
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Unsubscribe<IContactsVm.FriendSelected>(OnFriendSelected);
        }
    }
}
