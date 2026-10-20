using HomeNetOrm.Interfaces.Generators;

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// 🔒 СКРЫТАЯ РЕАЛИЗАЦИЯ: Чистый SQLite для локальных настроек девайса
    /// </summary>
    internal class SqliteDeviceSettingsSqlGenerator : ILocalDeviceSettingsSqlGenerator
    {
        public string GenerateSelectByKey()
        {
            // Берем значение из колонки "id" (куда маппится значение) по ключу в "device_id"
            return @"SELECT ""id"" FROM ""device_settings"" WHERE ""device_id"" = @Key LIMIT 1;";
        }

        public string GenerateInsertOrUpdate()
        {
            // Пишем: device_id — это наш строковый ключ, id — это наш сгенерированный GUID
            return @"INSERT INTO ""device_settings"" (""device_id"", ""id"") 
                     VALUES (@Key, @Value) 
                     ON CONFLICT(""device_id"") DO UPDATE SET ""id"" = EXCLUDED.""id"";";
        }
    }
}
