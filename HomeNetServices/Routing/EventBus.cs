using HomeNetCore.Interfaces.Events;
using HomeNetServices.Diagnostics;

namespace HomeNetServices.Routing
{
    public class EventBus : IEventBus
    {
        // Основная collection подписчиков шины
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
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Ошибка регистрации подписки: {ex.Message}");
            }
        }

        /// <summary>
        /// Публикация сигнала в воздух.
        /// </summary>
        public void Publish<TMessage>(object sender, TMessage message)
        {
            if (message == null) return;

            // 🔥 УЛУЧШЕНИЕ: Берем РЕАЛЬНЫЙ тип объекта рантайма вместо compile-time TMessage.
            // Это гарантирует, что инспектор увидит точное имя вложенного рекорда!
            var type = message.GetType();

            // 🛡️ БРОНЕЖИЛЕТ ДЛЯ ИНСПЕКТОРА
            try
            {
                // Инспектор за O(1) перехватывает "паспорта" типов и строит чертеж
                Inspector.RecordPublish(sender, type);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Не удалось залогировать публикацию: {ex.Message}");
            }

            // 🚀 КРИТИЧЕСКИЙ ПУТЬ: Доставка сигналов до подписчиков (выполняется железно)
            // Ищем подписчиков по точному типу сообщения
            if (_subscribers.TryGetValue(type, out var actions))
            {
                var actionsCopy = new List<object>(actions);
                foreach (var action in actionsCopy)
                {
                    ((Action<TMessage>)action)(message);
                }
            }
        }

        /// <summary>
        /// Отписка компонента от определенного типа сигнала.
        /// </summary>
        public void Unsubscribe<TMessage>(Action<TMessage> action)
        {
            if (action == null) return;

            var type = typeof(TMessage);

            if (_subscribers.TryGetValue(type, out var actions))
            {
                if (actions.Contains(action))
                {
                    actions.Remove(action);
                }

                if (actions.Count == 0)
                {
                    _subscribers.Remove(type);
                }
            }
        }
    }
}
