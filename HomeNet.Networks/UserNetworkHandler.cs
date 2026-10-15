using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;

namespace HomeNet.Network
{
    public class UserNetworkHandler : BaseNetworkHandler<UserEntity>
    {
        // Слушает создание/апдейт профилей с сервера
        public override string ServerMethodName => "ReceiveUserSync";

        public UserNetworkHandler(IEventBus eventBus) : base(eventBus) { }

        protected override Task HandleInternalAsync(UserEntity user)
        {
            // Выстреливаем в шину событием вставки пользователя
            // Проснется UserSynchronizer и обновит локальный список
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(user));
            return Task.CompletedTask;
        }
    }
}
