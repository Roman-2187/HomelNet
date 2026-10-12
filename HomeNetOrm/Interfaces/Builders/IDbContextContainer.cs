namespace HomeNetOrm.Interfaces.Builders
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
