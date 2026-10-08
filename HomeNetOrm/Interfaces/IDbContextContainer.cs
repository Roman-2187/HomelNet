namespace HomeNetOrm.Interfaces
{
    public interface IDbContextContainer
    {
        Task InitializeAllDatabasesAsync();
        bool IsPostgresAvailable { get; }

        /// <summary>
        /// Вложенный рекорд сигнала готовности Хаба
        /// </summary>
        public record InfrastructureReady();


    }
}
