using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.DBProviders.Sqlite.HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeSocialNetwork.Core; // Подключаем твой SchemaRegistry
using System;
using System.Linq;

namespace HomeNetCore.Data.DBProviders.Sqlite
{
    // 🧙‍♂️ Магический дженерик-генератор: штампует SQL под любой твой C#-класс!
    public class SqliteSqlGenerator<T> : ISqLiteSqlGenerator<T> where T : class
    {
        private readonly TableSchema _formattedTable;
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        // Конструктор теперь САМ забирает схему, убрали лишний параметр снаружи!
        public SqliteSqlGenerator(
            ISchemaAdapter adapter,
            ILogger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Автопилот: берём имя класса (например, "UserEntity")
            string targetEntityName = typeof(T).Name;

            // Отрезаем слово "Entity" для точного поиска в реестре (например, "UserEntity" -> "User")
            string cleanName = targetEntityName.Replace("Entity", "");

            // Ищем подходящую таблицу в SchemaRegistry (по имени "Users", "Messages" или "Friends")
            var rawTableSchema = SchemaRegistry.GetAllSchemas()
                .FirstOrDefault(s => s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                                     s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (rawTableSchema == null || string.IsNullOrEmpty(rawTableSchema.TableName))
            {
                _logger.LogError($"[КРИТ] В SchemaRegistry не найдена схема для класса {targetEntityName}!");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует в реестре схем.");
            }

            // Твой проверенный ночной перевод схемы в snake_case 🐍
            _formattedTable = adapter.ConvertToSnakeCaseSchema(rawTableSchema)
                ?? throw new InvalidOperationException("Ошибка адаптера при конвертации схемы");
        }

        // Универсальная вставка (CRUD - Create) 🚀
        public string GenerateInsert()
        {
            if (string.IsNullOrEmpty(_formattedTable.InsertFields) || string.IsNullOrEmpty(_formattedTable.InsertParameters))
            {
                throw new InvalidOperationException($"Некорректные поля для вставки в таблицу {_formattedTable.TableName}");
            }
            return $@"INSERT INTO {_formattedTable.TableName} ({_formattedTable.InsertFields}) VALUES ({_formattedTable.InsertParameters});
            SELECT last_insert_rowid() AS id";
        }

        // Универсальное обновление по ID (CRUD - Update) 🧼
        public string GenerateUpdate()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"У таблицы {_formattedTable.TableName} нет одиночного ID-столбца (возможно, ключ составной)");
            }

            if (string.IsNullOrEmpty(_formattedTable.SetClause))
            {
                throw new InvalidOperationException($"Некорректный SET clause для обновления таблицы {_formattedTable.TableName}");
            }

            return $"UPDATE {_formattedTable.TableName} SET {_formattedTable.SetClause} WHERE {idColumn} = @{idColumn}";
        }

        // Универсальное удаление по ID (CRUD - Delete) ❌
        public string GenerateDelete()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"У таблицы {_formattedTable.TableName} нет одиночного ID для удаления");
            }
            return $"DELETE FROM {_formattedTable.TableName} WHERE {idColumn} = @{idColumn}";
        }

        // Выборка по ID (CRUD - Read) 🔍
        public string GenerateSelectById()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"У таблицы {_formattedTable.TableName} нет одиночного ID для выборки");
            }
            return $"SELECT * FROM {_formattedTable.TableName} WHERE {idColumn} = @{idColumn}";
        }

        // Универсальный выбор всех строк из таблицы 📊
        public string GenerateSelectAll()
        {
            if (string.IsNullOrEmpty(_formattedTable.AllFields))
            {
                throw new InvalidOperationException($"Некорректные поля для выборки из таблицы {_formattedTable.TableName}");
            }

            // Твоё ночное исправление: оборачиваем имя таблицы в безопасные кавычки! ⚡
            return $"SELECT {_formattedTable.AllFields} FROM \"{_formattedTable.TableName}\"";
        }

        // =================================================================
        // 🔥 СПЕЦИАЛЬНЫЙ СЕКРЕТНЫЙ ОТСЕК ДЛЯ ТАБЛИЦ С EMAIL (Умный полиморфизм!)
        // =================================================================

        // Поиск по Email (сработает для пользователей на автопилоте!)
        public string GenerateSelectByEmail()
        {
            string emailColumn = GetEmailColumnOrThrow();
            return $"SELECT {_formattedTable.AllFields} FROM {_formattedTable.TableName} WHERE {emailColumn} = @{emailColumn}";
        }

        // Проверка существования Email
        public string GenerateEmailExists()
        {
            string emailColumn = GetEmailColumnOrThrow();
            return $"SELECT COUNT(*) FROM {_formattedTable.TableName} WHERE {emailColumn} = @email";
        }

        // Вспомогательный застрахованный метод, чтобы не дублировать код проверки ошибок 🛡️
        private string GetEmailColumnOrThrow()
        {
            if (_formattedTable.Columns == null)
            {
                throw new InvalidOperationException("В схеме таблицы отсутствуют колонки!");
            }

            string? emailColumn = _formattedTable.Columns.FirstOrDefault(c => c.Name == "email")?.Name;

            if (string.IsNullOrEmpty(emailColumn))
            {
                throw new InvalidOperationException($"Колонка 'email' не найдена в схеме сущности {typeof(T).Name}!");
            }

            return emailColumn;
        }
    }
}
