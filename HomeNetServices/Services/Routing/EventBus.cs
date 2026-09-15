using HomeNetServices.Services.Diagnostics;

namespace HomeNetServices.Services.Messaging
{

    public class EventBus : IEventBus
    {
        // Основная коллекция подписчиков шины
        private readonly Dictionary<Type, List<object>> _subscribers = new();

        // Наш автономный робот-картограф (Инспектор событий)
        public EventBusInspector Inspector { get; } = new();

        /// <summary>
        /// Подписка компонента на определенный тип сигнала.
        /// </summary>
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

            // 🧠 АВТО-РЕФЛЕКСИЯ: Вытаскиваем КТО и КАКИМ МЕТОДОМ подписался, чтобы занести в граф
            try
            {
                string controlName = action.Target?.GetType().Name ?? "UnknownSource";
                string methodName = action.Method.Name;

                // Передаем данные в объектный граф инспектора
                Inspector.RecordSubscribe(controlName, type, methodName);
            }
            catch (Exception ex)
            {
                // Защита: если рефлексия сбойнет, это не должно сорвать инициализацию приложения
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Ошибка регистрации подписки: {ex.Message}");
            }
        }

        /// <summary>
        /// Публикация сигнала в воздух.
        /// </summary>
        /// <param name="sender">Указывай 'this' (текущий экземпляр вьюмодели или контрола)</param>
        /// <param name="message">Сам объект-рекорд сообщения</param>
        public void Publish<TMessage>(object sender, TMessage message)
        {
            if (message == null) return;

            var type = typeof(TMessage);

            // 🛡️ БРОНЕЖИЛЕТ ДЛЯ ИНСПЕКТОРА
            try
            {
                // Инспектор за O(1) перехватывает "паспорта" типов и строит чертеж
                Inspector.RecordPublish(sender, type);
            }
            catch (Exception ex)
            {
                // Если профайлер упадет — пишем лог, но основное приложение продолжает жить!
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Не удалось залогировать публикацию: {ex.Message}");
            }

            // 🚀 КРИТИЧЕСКИЙ ПУТЬ: Доставка сигналов до подписчиков (выполняется железно)
            if (_subscribers.TryGetValue(type, out var actions))
            {
                // Делаем копию списка, чтобы избежать падений при изменении коллекции во время обхода
                var actionsCopy = new List<object>(actions);
                foreach (var action in actionsCopy)
                {
                    ((Action<TMessage>)action)(message);
                }
            }
        }
    }
}


