namespace HomeNet.Infrastructure.Shared.Interfaces.Routing
{
    /// <summary>
    /// 🔓 Открытая шина: Команды, Стейты, UI-события. Доступна всем компонентам.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<TMessage>(Action<TMessage> action);
        void SubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler);

        void Publish<TMessage>(object sender, TMessage message);
        Task PublishAsync<TMessage>(object sender, TMessage message);

        void Unsubscribe<TMessage>(Action<TMessage> action);
        void UnsubscribeAsync<TMessage>(Func<TMessage, Task> asyncHandler);
    }
}