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
    /// </summary>
    public partial class UserProfileViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private readonly IMessageRepository _messageRepo;

        // 🔥 НАШИ ДВА АВТОНОМНЫХ БЛОКА
        public ContactsListViewModel ContactsListVM { get; }
        public ChatViewModel ChatVm { get; }

        // Локальный стейт вкладки, скопированный из реактивного факта навигатора для XAML
        [ObservableProperty] private ClientSubTab _activeClientTab = ClientSubTab.None;

        public UserProfileViewModel(
            IEventBus eventBus,
            IMessageRepository messageRepo,
            ContactsListViewModel contactsListViewModel,
            ChatViewModel chatVm,
            UserNavigationManager navigation) : base(eventBus, navigation) // Передали навигатор в generic-базу
        {
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
            ContactsListVM = contactsListViewModel ?? throw new ArgumentNullException(nameof(contactsListViewModel));
            ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));

            // 🔥 Дублирование поля _eventBus удалено! Используем защищенное поле _eventBus базового класса.
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ЧИСТОТА ДЛЯ ИНСПЕКТОРА: Только жесткие ссылки на именованные методы! 🧼
            _eventBus.Subscribe<IUserVm.UserAuthenticated>(OnUserAuthenticated);
            _eventBus.Subscribe<IChatViewModel.NewSent>(OnNewMessageSent);
            _eventBus.Subscribe<IContactsListViewModel.FriendSelected>(OnFriendSelected);

            // Наш новый навигатор при изменении под-зон будет пулять этот факт в шину
            _eventBus.Subscribe<IUserVm.ClientTabChanged>(OnClientTabChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnUserAuthenticated(IUserVm.UserAuthenticated msg)
        {
            // Нам больше не нужно самим сохранять юзера, навигатор уже держит его в Navigation.CurrentUser!
            // Команда успешного входа автоматом переключит интерфейс в Messenger.
            ChatVm.IsChatOpen = false;
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

        private void OnFriendSelected(IContactsListViewModel.FriendSelected msg)
        {
            ChatVm.IsChatOpen = true;
        }

        #endregion

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose(); // Не забываем дернуть базовый деструктор ресурсы
            _eventBus.Unsubscribe<IUserVm.UserAuthenticated>(OnUserAuthenticated);
            _eventBus.Unsubscribe<IChatViewModel.NewSent>(OnNewMessageSent);
            _eventBus.Unsubscribe<IContactsListViewModel.FriendSelected>(OnFriendSelected);
            _eventBus.Unsubscribe<IUserVm.ClientTabChanged>(OnClientTabChanged);
        }

        #endregion
    }
}
