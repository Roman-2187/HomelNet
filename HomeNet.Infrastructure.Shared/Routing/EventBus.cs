using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Infrastructure.Shared.Diagnostics.EventInspector;

namespace HomeNet.Infrastructure.Shared.Routing
{
    // 🔥 Теперь класс реализует расширенный интерфейс
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _subscribers = new();
        public EventBusInspector Inspector { get; }

        public EventBus(EventBusInspector inspector)
        {
            Inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        }

        // ... (Методы Subscribe, SubscribeAsync, RegisterSubscriber остаются без изменений) ...

        public void Subscribe<TMessage>(Action<TMessage> action)
        {
            RegisterSubscriber(typeof(TMessage), action, action.Target, action.Method);
        }

        public void SubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler)
        {
            RegisterSubscriber(typeof(TMessage), asyncHandler, asyncHandler.Target, asyncHandler.Method);
        }

        private void RegisterSubscriber(Type messageType, object handler, object? target, System.Reflection.MethodInfo method)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_subscribers.ContainsKey(messageType)) _subscribers[messageType] = new List<object>();
            if (!_subscribers[messageType].Contains(handler)) _subscribers[messageType].Add(handler);

            try
            {
                Type subscriberType = target?.GetType() ?? method.DeclaringType ?? typeof(object);
                string componentName = subscriberType.Name.StartsWith("<") && subscriberType.DeclaringType != null
                    ? subscriberType.DeclaringType.Name : subscriberType.Name;
                Inspector.RecordSubscribe(componentName, messageType, method.Name, subscriberType);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Profiler Error] {ex.Message}"); }
        }

      

        public async Task PublishAsync<TMessage>(object sender, TMessage message)
        {
            if (message == null) return;
            var type = message.GetType();

            try { Inspector.RecordPublish(sender, type); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Profiler Error] {ex.Message}"); }

            if (_subscribers.TryGetValue(type, out var actions))
            {
                var actionsCopy = new List<object>(actions);
                foreach (var action in actionsCopy)
                {
                    try
                    {
                        if (action is Delegate del)
                        {
                            var result = del.DynamicInvoke(message);
                            if (result is Task task) await task;
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[EventBus Error] {ex.Message}"); }
                }
            }
        }

        public void Publish<TMessage>(object sender, TMessage message)
        {
            _ = PublishAsync(sender, message);
        }

        // ... (Методы Unsubscribe остаются без изменений) ...
        public void Unsubscribe<TMessage>(Action<TMessage> action) => UnsubscribeInternal(typeof(TMessage), action);
        public void UnsubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler) => UnsubscribeInternal(typeof(TMessage), asyncHandler);

        private void UnsubscribeInternal(Type messageType, object handler)
        {
            if (handler == null) return;
            if (_subscribers.TryGetValue(messageType, out var actions))
            {
                if (actions.Contains(handler)) actions.Remove(handler);
                if (actions.Count == 0)
                {
                    _subscribers.Remove(messageType);
                    Inspector.RecordUnsubscribe(messageType);
                }
            }
        }
    }
}