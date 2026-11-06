using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNet.Network.Network.ApiClient
{
    public interface IApiClientBase
    {
        bool IsServerAvailable { get; }
        // Можно добавить общие методы, если они нужны
    }

    public abstract class BaseApiClient : IApiClientBase
    {
        protected readonly HttpClient HttpClient;
        protected readonly ICastomLogger Logger;

        protected BaseApiClient(HttpClient httpClient, ICastomLogger logger)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsServerAvailable => throw new NotImplementedException("Реализуй проверку через пинг или заголовок");
    }
}
