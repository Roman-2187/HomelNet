namespace HomeNetCore.Interfaces.Events
{
    public interface IEventBus
    {
        void Publish<TMessage>(object sender, TMessage message);
        void Subscribe<TMessage>(Action<TMessage> action);

        void Unsubscribe<TMessage>(Action<TMessage> action);
    }
}