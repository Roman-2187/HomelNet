namespace HomeNet.Infrastructure.Shared.Interfaces.Routing
{
    public interface IEventBus
    {
        // 🔥 Добавляем пустые дефолтные реализации (тело {} или Task.CompletedTask)
        // Теперь любой класс может написать "implements IEventBus" и оставить его пустым!

        void Subscribe<TMessage>(Action<TMessage> action) { }
        void SubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler) { }

        Task PublishAsync<TMessage>(object sender, TMessage message) => Task.CompletedTask;
        void Publish<TMessage>(object sender, TMessage message) { }

        void Unsubscribe<TMessage>(Action<TMessage> action) { }
        void UnsubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler) { }
    }
}
