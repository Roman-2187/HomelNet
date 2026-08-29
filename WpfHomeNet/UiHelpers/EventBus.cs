using System.Windows;

namespace WpfHomeNet.Messaging
{
    // Идеальные, чистые конверты-сообщения для нашего автобуса:
    public record WindowPositionChangedMessage(double Left, double Top, double Width, double Height, bool IsLoaded);

    public record UserDeletedMessage(int UserId);
    public record UserAddedMessage(HomeNetCore.Models.UserEntity User);
    

    

    // Отрезаем все double-аргументы, оставляем чистый флаг видимости!
    public record LogWindowVisibilityChangedMessage(bool IsVisible);

    public record UserTableVisibilityChangedMessage(bool IsVisible);

    public record FormVisibilityChangedMessage(Type FormType, Visibility Visibility);


    public class EventBus
    {
        // Словарь хранит списки подписчиков, сгруппированных по типу сообщения
        private readonly Dictionary<Type, List<object>> _subscribers = new();

        // Метод подписки на конкретный тип сообщения
        


        public void Subscribe<TMessage>(Action<TMessage> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var type = typeof(TMessage);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<object>();
            }

            // ИСПРАВЛЕНИЕ: Защита от геометрической прогрессии!
            // Если такой метод уже подписан на это сообщение — игнорируем дубликат!
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

