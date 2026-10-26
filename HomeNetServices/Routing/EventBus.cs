using HomeNetCore.Interfaces.Events;
using HomeNetServices.Diagnostics;

namespace HomeNetServices.Routing
{
    public class EventBus : IEventBus
    {
        // Храним делегаты как есть (и Action<T>, и Func<T, Task>)
        private readonly Dictionary<Type, List<object>> _subscribers = new();

        // 🔥 ТЕПЕРЬ ЭТО ПРОСТО ССЫЛКА НА ИНЖЕКТИРОВАННЫЙ СИНГЛТОН
        public EventBusInspector Inspector { get; }

        // 🔥 Конструктор явно требует инспектор из DI контейнера
        public EventBus(EventBusInspector inspector)
        {
            Inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        }

        // --- 1. СИНХРОННАЯ ПОДПИСКА (Для вкладок, кнопок и стейта) ---
        public void Subscribe<TMessage>(Action<TMessage> action)
        {
            RegisterSubscriber(typeof(TMessage), action, action.Target, action.Method);
        }

        // --- 2. АСИНХРОННАЯ ПОДПИСКА (Для тяжелых задач, ОРМ и сети) ---
        public void SubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler)
        {
            RegisterSubscriber(typeof(TMessage), asyncHandler, asyncHandler.Target, asyncHandler.Method);
        }

        // Общий приватный метод регистрации для устранения дублирования кода
        private void RegisterSubscriber(Type messageType, object handler, object? target, System.Reflection.MethodInfo method)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_subscribers.ContainsKey(messageType))
            {
                _subscribers[messageType] = new List<object>();
            }

            if (!_subscribers[messageType].Contains(handler))
            {
                _subscribers[messageType].Add(handler);
            }

            try
            {
                // Находим тип самого класса-подписчика
                Type subscriberType = target?.GetType() ?? method.DeclaringType ?? typeof(object);

                string controlName = subscriberType.Name;
                string methodName = method.Name;

                // Передаем в инспектор — теперь он увидит и асинхронные методы!
                Inspector.RecordSubscribe(controlName, messageType, methodName, subscriberType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Ошибка регистрации подписки: {ex.Message}");
            }
        }

        // --- 3. АСИНХРОННАЯ ПУБЛИКАЦИЯ (Умеет дожидаться Task) ---
        public async Task PublishAsync<TMessage>(object sender, TMessage message)
        {
            if (message == null) return;

            var type = message.GetType();

            try
            {
                Inspector.RecordPublish(sender, type);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Не удалось залогировать публикацию: {ex.Message}");
            }

            if (_subscribers.TryGetValue(type, out var actions))
            {
                var actionsCopy = new List<object>(actions);
                foreach (var action in actionsCopy)
                {
                    try
                    {
                        if (action is Delegate del)
                        {
                            // Вызываем метод динамически
                            var result = del.DynamicInvoke(message);

                            // 🔥 КЛЮЧЕВАЯ ФИШКА: Если метод вернул Task (был асинхронным), дожидаемся его!
                            if (result is Task task)
                            {
                                await task;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EventBus Error] Сбой доставки сигнала: {ex.Message}");
                    }
                }
            }
        }

        // --- 4. СИНХРОННАЯ ПУБЛИКАЦИЯ (Для совместимости со старым кодом) ---
        public void Publish<TMessage>(object sender, TMessage message)
        {
            // Запускаем асинхронную публикацию "пожар и забыл" для синхронного контекста
            _ = PublishAsync(sender, message);
        }

        // --- 5. ОТПИСКА (Работает универсально для обоих типов) ---
        public void Unsubscribe<TMessage>(Action<TMessage> action)
        {
            UnsubscribeInternal(typeof(TMessage), action);
        }

        public void UnsubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler)
        {
            UnsubscribeInternal(typeof(TMessage), asyncHandler);
        }

        private void UnsubscribeInternal(Type messageType, object handler)
        {
            if (handler == null) return;

            if (_subscribers.TryGetValue(messageType, out var actions))
            {
                if (actions.Contains(handler))
                {
                    actions.Remove(handler);
                }

                if (actions.Count == 0)
                {
                    _subscribers.Remove(messageType);
                    Inspector.RecordUnsubscribe(messageType);
                }
            }
        }
    }
}
