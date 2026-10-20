namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// 🌐 УДАЛЕННЫЙ интерфейс генератора для ведения учета синхронизации на сервере
    /// </summary>
   
        public interface IRemoteDeviceSyncSqlGenerator
        {
            // Добавляем параметр строки во все контракты 🎯
            string GenerateDeviceSyncUpsert(string tableName);
            string GenerateSelectUpdatesByTimestamp(string tableName);
            string GenerateSelectUserIdByDevice(string tableName);
        }
    }

