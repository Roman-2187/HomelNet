

namespace HomeNetCore.Interfaces
{
    public interface IEventBus
    {
        void Publish<TMessage>(object sender, TMessage message);
        void Subscribe<TMessage>(Action<TMessage> action);
    }
}