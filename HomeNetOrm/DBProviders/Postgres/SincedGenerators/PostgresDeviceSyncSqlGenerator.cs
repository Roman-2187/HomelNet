using HomeNetOrm.Interfaces.Generators;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    /// <summary>
    /// 🔒 ЧИСТЫЙ ГЕНЕРАТОР: Серверная синхронизация девайсов на Postgres.
    /// Имя таблицы передается строкой прямо в методы. Никакого хардкода! 🎯
    /// </summary>
    internal class PostgresDeviceSyncSqlGenerator : IRemoteDeviceSyncSqlGenerator
    {
        public string GenerateDeviceSyncUpsert(string tableName)
        {
            // Экранируем переданное имя таблицы кавычками под Postgres
            string safeTableName = $"\"{tableName.Replace("\"", "")}\"";

            return $@"
                INSERT INTO {safeTableName} (id, device_id, user_id, last_synced_at, updated_at)
                VALUES (@Id, @deviceId, @userId, @Now, @Now)
                ON CONFLICT (user_id, device_id) 
                DO UPDATE SET last_synced_at = EXCLUDED.last_synced_at, updated_at = NOW();";
        }

        public string GenerateSelectUpdatesByTimestamp(string tableName)
        {
            string safeTableName = $"\"{tableName.Replace("\"", "")}\"";
            return $@"SELECT * FROM {safeTableName} WHERE updated_at > @clientCheckpoint;";
        }

        public string GenerateSelectUserIdByDevice(string tableName)
        {
            string safeTableName = $"\"{tableName.Replace("\"", "")}\"";
            return $@"SELECT user_id FROM {safeTableName} WHERE device_id = @deviceId LIMIT 1;";
        }
    }
}
