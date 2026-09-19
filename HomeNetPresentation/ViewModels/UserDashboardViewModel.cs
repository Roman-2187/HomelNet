using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Идеально пустая доска-контейнер клиентской зоны.
    /// </summary>
    public partial class UserDashboardViewModel : FormViewModelBase
    {
        private readonly IMessageRepository _messageRepo;

        // 🔥 НАШИ ДВА АВТОНОМНЫХ БЛОКА
        public ContactsListViewModel ContactsListVM { get; }
        public ChatViewModel ChatVm { get; }

        [ObservableProperty] private UserEntity? _currentUser;

        public UserDashboardViewModel(
            IEventBus eventBus,
            IMessageRepository messageRepo,
            ContactsListViewModel contactsListViewModel,
            ChatViewModel chatVm) : base(eventBus)
        {
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
            ContactsListVM = contactsListViewModel ?? throw new ArgumentNullException(nameof(contactsListViewModel));
            ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));


            IsControlVisible = false;

            // Слушаем логин только для того, чтобы поджечь флаг видимости самого дашборда на экране
            EventBus.Subscribe<IAuthenticationViewModel.UserLogged>(async msg => { CurrentUser = msg.User; IsControlVisible = true; await Task.CompletedTask; });
            EventBus.Subscribe<IUsersTableViewModel.Added>(async msg => { CurrentUser = msg.User; IsControlVisible = true; await Task.CompletedTask; });

            // 🔥 Перехватчик отправки сообщений из чата для записи в БД переезжает на связку с ContactsListVM
            EventBus.Subscribe<IChatViewModel.NewSent>(async msg =>
            {
                if (CurrentUser == null) return;

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
                    // 1. Сохраняем эсэмэску в SQLite / Postgres
                    await _messageRepo.SaveMessageAsync(entity);

                    // 2. Пушим в визуальную ленту чата, если открыт именно этот собеседник
                    if (ContactsListVM.SelectedFriend != null && ContactsListVM.SelectedFriend.Id == msg.ReceiverId)
                    {
                        ChatVm.Messages.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[БЭКЕНД ЧАТА СБОЙ]: {ex.Message}"));
                }
            });

            // Синхронизируем анимацию открытия чата: когда в левой панели выбрали друга, правая панель взлетает
            EventBus.Subscribe<IContactsListViewModel.FriendSelected>(msg => ChatVm.IsChatOpen = true);
        }
    }
}
