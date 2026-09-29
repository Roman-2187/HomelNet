using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Тотально изолированная и чистая панель пользователя SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера навигации.
    /// Управляется новыми браузерными веб-ивентами авторизации и регистрации.
    /// </summary>
    public partial class UserViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private readonly IMessageRepository _messageRepo;

        // 🔥 НАШИ ДВА АВТОНОМНЫХ БЛОКА
        public ContactsViewModel ContactsListVM { get; }
        public ChatViewModel ChatVm { get; }

        private int _currentUserId; // 🔥 Наш локальный UI-кэш для ID текущего юзера

        // Локальный стейт вкладки, скопированный из реактивного факта навигатора для XAML
        [ObservableProperty] private ClientSubTab _activeClientTab = ClientSubTab.None;

        public UserViewModel(
            IEventBus eventBus,
            IMessageRepository messageRepo,
            ContactsViewModel contactsListViewModel,
            ChatViewModel chatVm,
            UserNavigationManager navigation) : base(eventBus, navigation) // Передали навигатор в generic-базу
        {
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
            ContactsListVM = contactsListViewModel ?? throw new ArgumentNullException(nameof(contactsListViewModel));
            ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));

            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ВЕБ-СТАНДАРТ: Слушаем раздельные каналы входа и регистрации для Инспектора! 🧼
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUp);

            _eventBus.Subscribe<IChatViewModel.NewSent>(OnNewMessageSent);
            _eventBus.Subscribe<IContactsVm.FriendSelected>(OnFriendSelected);

            // Наш новый навигатор при изменении под-зон будет пулять этот факт в шину
            _eventBus.Subscribe<IUserVm.ClientTabChanged>(OnClientTabChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id; // 🔥 Запомнили, кто сидит за рулем мессенджера!
            ChatVm.IsChatOpen = false;
            ContactsListVM.SetCurrentUser(msg.User);
        }

        private void OnUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id; // Запомнили при регистрации
            ChatVm.IsChatOpen = false;
            ContactsListVM.SetCurrentUser(msg.User);
        }

        private void OnClientTabChanged(IUserVm.ClientTabChanged msg)
        {
            if (msg == null) return;

            // Ловим пулю от навигатора и сохраняем стейт вкладки у себя для триггеров UI
            ActiveClientTab = msg.ActiveTab;
        }

        /// <summary>
        /// 🔥 Перехватчик отправки сообщений из чата для записи в БД
        /// </summary>
        private async void OnNewMessageSent(IChatViewModel.NewSent msg)
        {
            // ЖЕСТКИЙ КОНТРОЛЬ: Берем ID текущего юзера прямо из синглтон-навигатора базового класса!
            if (msg == null || Navigation.CurrentUser == null) return;

            var entity = new MessageEntity
            {
                SenderId = Navigation.CurrentUser.Id, // Прямой и безопасный доступ без кастов
                ReceiverId = msg.ReceiverId,
                Text = msg.Text,
                MediaType = "Text",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // 1. Сохраняем эсэмэску в SQLite / Postgres через репозиторий
                await _messageRepo.SaveMessageAsync(entity);

                // 2. Пушим в визуальную ленту чата, если открыт именно этот собеседник
                if (ContactsListVM.SelectedFriend != null && ContactsListVM.SelectedFriend.Id == msg.ReceiverId)
                {
                    ChatVm.Messages.Add(entity);
                }
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[БЭКЕНД ЧАТА СБОЙ]: {ex.Message}"));
            }
        }

        private void OnFriendSelected(IContactsVm.FriendSelected msg)
        {
            ChatVm.IsChatOpen = true;
        }

        #endregion

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose(); // Чистим базовые ресурсы

            // 🔥 Обновили отписки на новые рекорды, чтобы не было утечек памяти!
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUp);

            _eventBus.Unsubscribe<IChatViewModel.NewSent>(OnNewMessageSent);
            _eventBus.Unsubscribe<IContactsVm.FriendSelected>(OnFriendSelected);
            _eventBus.Unsubscribe<IUserVm.ClientTabChanged>(OnClientTabChanged);
        }

        #endregion
    }
}
