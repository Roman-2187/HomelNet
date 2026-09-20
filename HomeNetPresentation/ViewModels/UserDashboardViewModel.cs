using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Чистая доска-контейнер клиентской зоны мессенджера SiberNet.
    /// Полностью управляется через шину событий и глобальный автомат энумов.
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
            ChatViewModel chatVm, NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
            ContactsListVM = contactsListViewModel ?? throw new ArgumentNullException(nameof(contactsListViewModel));
            ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));

            // Стерильно ловим логин, чтобы просто запомнить текущую сессию пользователя
            EventBus.Subscribe<IAuthenticationViewModel.UserLogged>(async msg =>
            {
                CurrentUser = msg.User;
                await Task.CompletedTask;
            });

            EventBus.Subscribe<IUsersTableViewModel.Added>(async msg =>
            {
                CurrentUser = msg.User;
                await Task.CompletedTask;
            });

            // 🔥 Перехватчик отправки сообщений из чата для записи в БД
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
