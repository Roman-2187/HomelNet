namespace HomeNetCore.Interfaces.Events
{
    public interface IEventBus
    {
        // 🔥 ДЕФОЛТНАЯ РЕАЛИЗАЦИЯ: Пустые скобки означают, что метод по умолчанию НИЧЕГО не делает!
        void Publish<TMessage>(object sender, TMessage message) { }

        void Subscribe<TMessage>(Action<TMessage> action) { }

        void Unsubscribe<TMessage>(Action<TMessage> action) { }
    }
}
