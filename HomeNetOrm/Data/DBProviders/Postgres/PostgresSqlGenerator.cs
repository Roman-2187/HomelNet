using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetOrm.Data.Schemes.CreateSchemaBd;
using HomeNetOrm.Interfaces;

namespace HomeNetOrm.Data.DBProviders.Postgres
{
    // 🧙‍♂️ Магический дженерик-генератор для PostgreSQL: штампует идеальный SQL в snake_case!
    public class PostgresSqlGenerator<T> : ISqlGenerator<T> where T : class
    {
        private readonly TableSchema _formattedTable;
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        public PostgresSqlGenerator(
            ISchemaAdapter adapter,
            ILogger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Автопилот: определяем имя сущности C# (например, "MessageEntity")
            string targetEntityName = typeof(T).Name;
            string cleanName = targetEntityName.Replace("Entity", "");

            // Ищем метаданные в едином реестре схем
            var rawTableSchema = SchemaRegistry.GetAllSchemas()
                .FirstOrDefault(s => s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                                     s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (rawTableSchema == null || string.IsNullOrEmpty(rawTableSchema.TableName))
            {
                _logger.LogError($"[КРИТ] В SchemaRegistry не найдена схема для класса {targetEntityName}!");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует в реестре схем.");
            }

            // Переводим C# схему в формат PostgreSQL (snake_case со всеми вытекающими) 🐍
            _formattedTable = adapter.ConvertToSnakeCaseSchema(rawTableSchema)
                ?? throw new InvalidOperationException("Ошибка адаптера при конвертации схемы для Postgres");
        }



        // =================================================================
        // 🔥 СПЕЦИАЛЬНЫЙ СЕКРЕТНЫЙ ОТСЕК ДЛЯ ЧАТА В POSTGRES
        // =================================================================
        public string GenerateSelectChatHistory()
        {
            if (_formattedTable.Columns == null)
            {
                throw new InvalidOperationException("В схеме таблицы отсутствуют колонки!");
            }

            bool hasSender = _formattedTable.Columns.Any(c => c.Name == "sender_id");
            bool hasReceiver = _formattedTable.Columns.Any(c => c.Name == "receiver_id");

            if (!hasSender || !hasReceiver)
            {
                throw new InvalidOperationException($"Сущность {typeof(T).Name} не поддерживает историю чата!");
            }

            // Штампуем чистый Postgres-SQL (все таблицы оборачиваем в кавычки по твоему канону)
            return $@"SELECT * FROM ""{_formattedTable.TableName}"" 
                      WHERE (sender_id = @userId AND receiver_id = @friendId) 
                         OR (sender_id = @friendId AND receiver_id = @userId)
                      ORDER BY created_at ASC;";
        }


        // Вставка (CRUD - Create) с фирменным синтаксисом RETURNING 🚀
        public string GenerateInsert()
        {
            if (string.IsNullOrEmpty(_formattedTable.InsertFields) || string.IsNullOrEmpty(_formattedTable.InsertParameters))
            {
                throw new InvalidOperationException($"Некорректные поля для вставки в таблицу {_formattedTable.TableName}");
            }

            // В Postgres вместо SQLite-вского SELECT last_insert_rowid() используется элегантный RETURNING id!
            string idColumn = _formattedTable.IdColumnName ?? "id";
            return $@"INSERT INTO ""{_formattedTable.TableName}"" ({_formattedTable.InsertFields}) 
                      VALUES ({_formattedTable.InsertParameters}) 
                      RETURNING {idColumn};";
        }

        // Обновление по ID (CRUD - Update) 🧼
        public string GenerateUpdate()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"У таблицы {_formattedTable.TableName} нет одиночного ID-столбца");
            }

            if (string.IsNullOrEmpty(_formattedTable.SetClause))
            {
                throw new InvalidOperationException($"Некорректный SET clause для таблицы {_formattedTable.TableName}");
            }

            return $"UPDATE \"{_formattedTable.TableName}\" SET {_formattedTable.SetClause} WHERE {idColumn} = @{idColumn};";
        }

        // Удаление по ID (CRUD - Delete) ❌
        public string GenerateDelete()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"У таблицы {_formattedTable.TableName} нет одиночного ID для удаления");
            }
            return $"DELETE FROM \"{_formattedTable.TableName}\" WHERE {idColumn} = @{idColumn};";
        }

        // Выборка по ID (CRUD - Read) 🔍
        public string GenerateSelectById()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"У таблицы {_formattedTable.TableName} нет одиночного ID для выборки");
            }
            return $"SELECT {_formattedTable.AllFields} FROM \"{_formattedTable.TableName}\" WHERE {idColumn} = @{idColumn};";
        }

        // Выборка всех строк 📊
        public string GenerateSelectAll()
        {
            if (string.IsNullOrEmpty(_formattedTable.AllFields))
            {
                throw new InvalidOperationException($"Некорректные поля для выборки из таблицы {_formattedTable.TableName}");
            }
            return $"SELECT {_formattedTable.AllFields} FROM \"{_formattedTable.TableName}\"";
        }

        // Мощный Postgres Upsert (вставка или обновление при конфликте ключей) 🔄
        public string GenerateUpsert()
        {
            string? idColumn = _formattedTable.IdColumnName;
            if (string.IsNullOrEmpty(idColumn))
            {
                throw new InvalidOperationException($"Upsert невозможен: у таблицы {_formattedTable.TableName} нет явного ID");
            }

            return $@"INSERT INTO ""{_formattedTable.TableName}"" ({_formattedTable.InsertFields}) 
                      VALUES ({_formattedTable.InsertParameters})
                      ON CONFLICT ({idColumn}) 
                      DO UPDATE SET {_formattedTable.SetClause}
                      RETURNING {idColumn};";
        }

        // Поиск по Email 📧
        public string GenerateSelectByEmail()
        {
            string emailColumn = GetEmailColumnOrThrow();
            return $"SELECT {_formattedTable.AllFields} FROM \"{_formattedTable.TableName}\" WHERE {emailColumn} = @{emailColumn};";
        }

        // Проверка существования Email 🔒
        public string GenerateEmailExists()
        {
            string emailColumn = GetEmailColumnOrThrow();
            // Передаем параметр @email как в SQLite для полной совместимости Dapper-шлейфов
            return $"SELECT COUNT(*) FROM \"{_formattedTable.TableName}\" WHERE {emailColumn} = @email;";
        }

        public string GenerateSelectCount()
        {
            return $"SELECT COUNT(*) FROM \"{_formattedTable.TableName}\"";
        }

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
