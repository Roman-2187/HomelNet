using System.Windows;

namespace WpfHomeNet.Messaging
{

        // =================================================================================
        // РЕАКТИВНЫЕ СИГНАЛЫ СИСТЕМЫ (КОНВЕРТЫ ДЛЯ ШИНЫ EVENTBUS)
        // =================================================================================

        /// <summary>
        /// Сигнал изменения геометрии главного окна.
        /// MainWindow шлёпает его при каждом сдвиге или ресайзе, а LogViewModel ловит из воздуха
        /// и плавно двигает рамку окна логов следом.
        /// </summary>
        public record WindowPositionChangedMessage(double Left, double Top, double Width, double Height, bool IsLoaded);

        /// <summary>
        /// Сигнал об успешном удалении пользователя из базы данных.
        /// Швыряется сервисом удаления. По нему MainViewModel убирает юзера из коллекции на экране,
        /// а автономный статус-бар уменьшает общий счётчик.
        /// </summary>
        public record UserDeletedMessage(int UserId);

        /// <summary>
        /// Сигнал о регистрации и добавлении нового пользователя в СУБД.
        /// Кладётся в шину формой регистрации. MainViewModel ловит его и докидывает юзера в общую таблицу,
        /// а статус-бар инкрементирует счётчик и пишет имя новичка.
        /// </summary>
        public record UserAddedMessage(HomeNetCore.Models.UserEntity User);

        /// <summary>
        /// Сигнал принудительного переключения видимости окна отладочных логов (LogWindow).
        /// </summary>
        public record LogWindowVisibilityChangedMessage(bool IsVisible);

        /// <summary>
        /// Сигнал управления отображением центральной таблицы пользователей.
        /// </summary>
        public record UserTableVisibilityChangedMessage(bool IsVisible);

        /// <summary>
        /// Сигнал о полной перезагрузке и обновлении списка пользователей из SQLite.
        /// Срабатывает один раз при старте ядра базы данных. Сигнализирует статус-бару
        /// стартовое количество заложников-админа.
        /// </summary>
        public record UsersListRefreshedMessage(System.Collections.ObjectModel.ObservableCollection<HomeNetCore.Models.UserEntity> Users);

        /// <summary>
        /// Универсальный переключатель видимости диалоговых окон (Регистрация, Авторизация, Удаление).
        /// Вместо хардкодных строк передаёт строгий системный тип вьюмодели (Type), по которому
        /// формы синхронно гасят друг друга, а автономный статус-бар определяет, какая форма сейчас открыта.
        /// </summary>
        public record FormVisibilityChangedMessage(Type FormType, System.Windows.Visibility Visibility);

        /// <summary>
        /// Сигнал от верхней кнопки-тумблера "🛠️ Админка".
        /// Ловится напрямую методом InitializeBusSubscriptions внутри MainViewModel
        /// и заставляет боковую панель админа мгновенно свернуться или развернуться.
        /// </summary>
        public record AdminMenuVisibilityChangedMessage(bool IsVisible);

        /// <summary>
        /// Прямой текстовый приказ для строки состояния.
        /// Используется для вывода разовых глобальных уведомлений, например, при успешном выходе из аккаунта.
        /// </summary>
        public record StatusTextChangedMessage(string NewStatus);
    



    public class EventBus
    {
        private readonly Dictionary<Type, List<object>> _subscribers = new();

        public void Subscribe<TMessage>(Action<TMessage> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var type = typeof(TMessage);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<object>();
            }

            if (!_subscribers[type].Contains(action))
            {
                _subscribers[type].Add(action);
            }
        }

        // Метод публикации сообщения "в воздух"
        public void Publish<TMessage>(TMessage message)
        {
            if (message == null) return;

            var type = typeof(TMessage);
            if (_subscribers.TryGetValue(type, out var actions))
            {         
                var actionsCopy = new List<object>(actions);
                foreach (var action in actionsCopy)
                {
                    ((Action<TMessage>)action)(message);
                }
            }
        }
    }
}

