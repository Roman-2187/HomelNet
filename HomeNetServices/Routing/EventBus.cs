using HomeNetCore.Interfaces.Events;
using HomeNetServices.Diagnostics;
using System;
using System.Collections.Generic;

namespace HomeNetServices.Routing
{
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _subscribers = new();

        // 🔥 ТЕПЕРЬ ЭТО ПРОСТО ССЫЛКА НА ИНЖЕКТИРОВАННЫЙ СИНГЛТОН
        public EventBusInspector Inspector { get; }

        // 🔥 Конструктор явно требует инспектор из DI контейнера
        public EventBus(EventBusInspector inspector)
        {
            Inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        }

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

            try
            {
                string controlName = action.Target?.GetType().Name ?? "UnknownSource";
                string methodName = action.Method.Name;

                // Заносим в единственный, общий граф синглтона 🧼
                Inspector.RecordSubscribe(controlName, type, methodName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Ошибка регистрации подписки: {ex.Message}");
            }
        }

       





        public void Publish<TMessage>(object sender, TMessage message)
        {
            if (message == null) return;

            // Получаем точный тип сообщения в рантайме
            var type = message.GetType();

            // 🛡️ Фиксируем публикацию в инспекторе
            try
            {
                // Теперь инспектор ЖЕЛЕЗНО увидит твой UserService! 🧼
                Inspector.RecordPublish(sender, type);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profiler Error] Не удалось залогировать публикацию: {ex.Message}");
            }

            // 🚀 ДИНАМИЧЕСКАЯ ДОСТАВКА СИГНАЛОВ
            if (_subscribers.TryGetValue(type, out var actions))
            {
                var actionsCopy = new List<object>(actions);
                foreach (var action in actionsCopy)
                {
                    try
                    {
                        // Используем DynamicInvoke вместо жесткого каста (Action<TMessage>)
                        // Это спасет от коллизий интерфейсов и конкретных типов рекордов!
                        if (action is Delegate del)
                        {
                            del.DynamicInvoke(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EventBus Error] Сбой доставки сигнала: {ex.Message}");
                    }
                }
            }
        }

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
