using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers.Exceptions;
using System.Data;
using System.Data.Common;

namespace HomeNetCore.Data.DBProviders
{
    public class GenericSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _sqlInit;
        private readonly ISchemaAdapter _adapter;
        private readonly DbConnection _requiredConnection;
        private readonly ILogger _logger;

        public GenericSchemaProvider(
            ISchemaSqlInitializer sqlInit,
            ISchemaAdapter adapter,
            DbConnection connection,
            ILogger logger)
        {
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlInit = sqlInit ?? throw new ArgumentNullException(nameof(sqlInit));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым", nameof(tableName));

            // Защита: гарантируем, что сетевой шлейф открыт перед отправкой запроса к метаданным! 🔥
            if (_requiredConnection.State != ConnectionState.Open)
            {
                await _requiredConnection.OpenAsync();
            }

            var rawColumnsData = new List<(string Name, string DataType, bool IsNullable, string KeyType, string ExtraInfo)>();

            try
            {
                using var command = _requiredConnection.CreateCommand();
                command.CommandText = _sqlInit.GenerateGetTableStructureSql(tableName);

                // Если это Postgres, добавляем параметр для защиты от инъекций
                if (command.Parameters.Contains("@tableName") == false)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = "@tableName";
                    param.Value = tableName;
                    command.Parameters.Add(param);
                }

                // БЫСТРЫЙ ИЗОЛИРОВАННЫЙ ЧИТАТЕЛЬ: Выгребаем сырые строки метаданных в память СУБД ⚡
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rawColumnsData.Add((
                            Name: reader.IsDBNull(_adapter.NameIndex) ? string.Empty : reader.GetString(_adapter.NameIndex),
                            DataType: reader.IsDBNull(_adapter.TypeIndex) ? string.Empty : reader.GetString(_adapter.TypeIndex),

                            // Для SQLite это булево/инт инверсия, для Postgres — строка 'YES'/'NO'. 
                            // Проще всего заставить адаптер или общую логику читать строку/объект универсально!
                            IsNullable: ReadNullable(reader, _adapter.NullableIndex),

                            KeyType: ReadStringUniversal(reader, _adapter.PrimaryKeyIndex),
                            ExtraInfo: _adapter.ExtraInfoIndex >= 0 && !reader.IsDBNull(_adapter.ExtraInfoIndex)
                                ? reader.GetValue(_adapter.ExtraInfoIndex)?.ToString() ?? string.Empty
                                : string.Empty
                        ));
                    }
                } // Ридер закрыт, канал связи полностью свободен! 🔐

                // Спокойно маппим данные в наши умные C#-модели
                var columns = new List<ColumnSchema>();
                foreach (var row in rawColumnsData)
                {
                    bool isPk = row.KeyType.Equals("primary", StringComparison.OrdinalIgnoreCase) ||
                                row.KeyType.Equals("1") || row.KeyType.Equals("true");

                    isPk = false;
                    columns.Add(new ColumnSchema
                    {
                        Name = row.Name,
                        OriginalName = row.Name,
                        Type = _adapter.MapDbSpecificationType(row.DataType), // Адаптер рулит типами! 💎
                        IsNullable = row.IsNullable,
                        IsPrimaryKey = isPk,
                        IsAutoIncrement = row.ExtraInfo.Contains("nextval") || row.ExtraInfo.Equals("auto_increment", StringComparison.OrdinalIgnoreCase)
                    });
                }

                _logger.LogDebug($"Получено {columns.Count} столбцов для таблицы {tableName}");

                // Сценарий 1: База вообще ничего не вернула (совсем пусто)
                if (columns.Count == 0)
                {
                    _logger.LogError($"[КРИТИЧЕСКАЯ ОШИБКА] Таблица '{tableName}' не найдена в БД или запрос метаданных вернул пустой результат!");
                    return new TableSchema { TableName = tableName ?? string.Empty };
                }

                var getSchema = new TableSchema { TableName = tableName ?? string.Empty, Columns = columns };

                // Сценарий 2: Таблица есть, но фейсконтроль парсинга PK не пройден
                if (!getSchema.Initialize())
                {
                    _logger.LogWarning($"[ПРЕДУПРЕЖДЕНИЕ] В таблице '{tableName}' найдено {columns.Count} колонок, но не удалось распознать Primary Key (ID). Проверьте маппинг типов!");
                }
                else
                {
                    _logger.LogDebug($"Получено имен колонок таблицы {tableName} : {getSchema.columnNames}");
                }

                return getSchema;


         
            }
            catch (Exception ex)
            {
                throw new SchemaProviderException(
                    $"Ошибка при получении схемы для таблицы {tableName}: {ex.Message}", ex);
            }
        }

        private bool ReadNullable(DbDataReader reader, int index)
        {
            if (index < 0 || reader.IsDBNull(index)) return false;

            // 🔥 ИСПРАВЛЕНИЕ: Чётко кастим к string и подстраховываемся через ?? string.Empty
            string val = reader.GetValue(index)?.ToString() ?? string.Empty;

            if (val.Equals("YES", StringComparison.OrdinalIgnoreCase)) return true;
            if (val.Equals("NO", StringComparison.OrdinalIgnoreCase)) return false;

            // SQLite логика инверсии notnull (если в базе 1, то это NOT NULL = true, значит IsNullable = false)
            if (bool.TryParse(val, out bool res)) return !res;

            return val.Equals("0");
        }


        private string ReadStringUniversal(DbDataReader reader, int index)
        {
            if (index < 0 || reader.IsDBNull(index)) return string.Empty;
            return reader.GetValue(index).ToString() ?? string.Empty;
        }
    }
}

