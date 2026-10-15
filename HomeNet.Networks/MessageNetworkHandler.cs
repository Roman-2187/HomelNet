using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;



namespace HomeNet.Network
{
    

    namespace HomeNet.Network
    {
        public class MessageNetworkHandler : BaseNetworkHandler<MessageEntity>
        {
            // Этот класс слушает строго метод "ReceiveMessage" от сервера
            public override string ServerMethodName => "ReceiveMessage";

            public MessageNetworkHandler(IEventBus eventBus) : base(eventBus) { }

            protected override Task HandleInternalAsync(MessageEntity message)
            {
                // Со всей дури швыряем объект сообщения в локальный автобус!
                // Твой MessageSynchronizer перехватит его и сохранит в SQLite
                EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));
                return Task.CompletedTask;
            }
        }
    }

}
