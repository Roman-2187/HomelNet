using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Network;

namespace HomeNet.Network
{
    public abstract class BaseNetworkHandler<TPayload> : INetworkHandler
    {
        protected readonly IEventBus EventBus;

        public abstract string ServerMethodName { get; }

        protected BaseNetworkHandler(IEventBus eventBus)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // Реализация интерфейса: принимает сырой объект, приводит к типу и запускает логику
        public Task HandleAsync(object payload)
        {
            if (payload is TPayload typedPayload)
            {
                return HandleInternalAsync(typedPayload);
            }
            return Task.CompletedTask;
        }

        // Каждый конкретный обработчик сам решит, какое событие кинуть в автобус
        protected abstract Task HandleInternalAsync(TPayload payload);
    }
}
