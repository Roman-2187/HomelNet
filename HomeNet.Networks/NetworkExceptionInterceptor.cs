using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Network;

namespace HomeNet.Network
{
    public class NetworkExceptionInterceptor : DelegatingHandler
    {
        private readonly IEventBus _eventBus;

        public NetworkExceptionInterceptor(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Пропускаем сетевой запрос дальше по кабелю
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception)
            {
                // 🔥 АВТОМАТИКА: Поймали ЛЮБОЙ сетевой сбой (таймаут, отказ сети, обрыв)
                // Моментально гасим статус-бар в красный OFFLINE одной строчкой на всё приложение!
                _eventBus.Publish(this, new NetworkStatusChanged(false));

                throw; // Пробрасываем ошибку дальше, чтобы репозиторий о ней знал
            }
        }
    }
}
