using System;
using System.Threading.Tasks;

namespace HomeNetCore.Interfaces.Network
{
    /// <summary>
    /// Общий контракт для любого сетевого обработчика событий от сервера
    /// </summary>
    public interface INetworkHandler
    {
        // Имя команды, которую обработчик ждет от сервера (например, "ReceiveMessage")
        string ServerMethodName { get; }

        // Метод, который вызовется автоматически при прилете JSON-строки
        Task HandleAsync(object payload);
    }
}
