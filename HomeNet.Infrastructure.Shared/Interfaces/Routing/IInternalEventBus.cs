namespace HomeNet.Infrastructure.Shared.Interfaces.Routing
{
    /// <summary>
    /// 🔒 Закрытая шина: ТОЛЬКО ФАКТЫ. Доступна репозиториям и синхронизаторам.
    /// </summary>
    public interface IInternalEventBus : IEventBus
    {
        // Тот самый метод, который открывает дверь в аппаратную
        void PublishFact<TMessage>(object sender, TMessage message);
    }
}