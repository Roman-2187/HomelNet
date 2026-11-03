using System;
using System.Threading.Tasks;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;

namespace HomeNet.Infrastructure.Shared.Utils
{
    /// <summary>
    /// Автоматический шлюз Request-Reply над шиной событий.
    /// Берет на себя всю нагрузку по временной подписке и гарантированной отписке! 🧼
    /// </summary>
    public class EventBusBox<TRequest, TResponse>
    {
        private readonly IEventBus _eventBus;
        private readonly Func<TResponse, bool> _matchPredicate;
        private TaskCompletionSource<TResponse>? _tcs;

        public EventBusBox(IEventBus eventBus, Func<TResponse, bool> matchPredicate)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _matchPredicate = matchPredicate ?? throw new ArgumentNullException(nameof(matchPredicate));
        }

        /// <summary>
        /// Отправляет запрос в шину и асинхронно ждет ответ, контролируя память
        /// </summary>
        public async Task<TResponse> SendAsync(object sender, TRequest request)
        {
            _tcs = new TaskCompletionSource<TResponse>();

            // 1. Подписываем ЧЕСТНЫЙ метод класса. Инспектор рантайма увидит это имя!
            _eventBus.Subscribe<TResponse>(OnResponseReceived);

            // 2. Публикуем запрос в сторону хэндлеров сети
            _eventBus.Publish(sender, request!);

            // 3. Замораживаем await, пока сеть шуршит байтами
            return await _tcs.Task;
        }

        /// <summary>
        /// Честный, именованный обработчик ответа для шины и инспектора
        /// </summary>
        public void OnResponseReceived(TResponse response)
        {
            // Проверяем предикатом, наш ли это пакет (например, совпал ли email)
            if (response == null || !_matchPredicate(response)) return;

            // ИСЧЕЗАЮЩАЯ ПОДПИСКА: Задача выполнена — мгновенно убираем себя из шины!
            _eventBus.Unsubscribe<TResponse>(OnResponseReceived);

            // Пропускаем результат в зависший await
            _tcs?.TrySetResult(response);
        }
    }
}
