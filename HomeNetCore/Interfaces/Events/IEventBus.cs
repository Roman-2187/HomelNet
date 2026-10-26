namespace HomeNetCore.Interfaces.Events
{
    public interface IEventBus
    {
        void Subscribe<TMessage>(Action<TMessage> action) { }

        // 🔥 Добавляем новый метод в контракт
        void SubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler) { }

        void Publish<TMessage>(object sender, TMessage message) { }
        Task PublishAsync<TMessage>(object sender, TMessage message) { return Task.CompletedTask; }

        void Unsubscribe<TMessage>(Action<TMessage> action) { }
    }
}
