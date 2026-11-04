namespace HomeNet.Synchronization.Interfaces.Sinc
{
    /// <summary>
    /// Глобальный диспетчер SiberNet Engine. 
    /// Отвечает только за запуск и контроль общего цикла репликации. 🧼🦾
    /// </summary>
    public interface ISyncCoordinator
    {
        Task StartAsync();
    }
}
