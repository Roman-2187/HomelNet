using Dapper;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using HomeNetCore.Helpers;
using System.Data;
using System.Data.Common;
using WpfHomeNet.Data.DBProviders.SqliteClasses;
using WpfHomeNet.Data.Schemes.CheckTableBd;
public class DBTableInitializer
{
    private readonly DbConnection _dbConnection;
    private readonly ISchemaSqlInitializer _schemaSqlGenerator;
    private readonly ILogger _logger;
    private readonly IGetSchemaProvider _schemaProvider;
    private readonly TableSchema _tableSchema;
    private readonly ISchemaAdapter _schemaAdapter;

    public DBTableInitializer
                      (
                        DbConnection connection,
                        IGetSchemaProvider schemaProvider,
                        ISchemaAdapter schemaAdapter,       
                        ISchemaSqlInitializer schemaSqlGenerator,
                        TableSchema tableSchema,
                        ILogger logger
                      )
    {
         _schemaProvider = schemaProvider ??
            throw new ArgumentNullException(nameof(schemaProvider));
        _dbConnection = connection ??
            throw new ArgumentNullException(nameof(connection));
        _schemaSqlGenerator = schemaSqlGenerator ??
            throw new ArgumentNullException(nameof(schemaSqlGenerator));
        _tableSchema = tableSchema ??
            throw new ArgumentNullException(nameof(tableSchema));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
        _schemaAdapter = schemaAdapter ??
            throw new ArgumentNullException(nameof(schemaAdapter));
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation($"Инициализация БД: проверка таблицы {_tableSchema.TableName} ...");

        if (!await TableExistsAsync())
        {
            await CreateTableAsync();
        }

        else
        {
            _logger.LogDebug($"Таблица {_tableSchema.TableName} существует ");
            await CheckTableStructureAsync();
        }
    }

    private async Task<bool> TableExistsAsync()
    {
        var sql = _schemaSqlGenerator.GenerateTableExistsSql(_tableSchema.TableName);

        try
        {
            var result = await _dbConnection.ExecuteScalarAsync<int>
                (sql, new { tableName = _tableSchema.TableName });
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при проверке таблицы: {ex.Message}");
            return false;
        }
    }

    private async Task CreateTableAsync()
    {
        _logger.LogInformation($"Таблица {_tableSchema.TableName} не найдена. Создаю новую...");
        try
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }
            await _dbConnection.ExecuteAsync(_schemaSqlGenerator.GenerateCreateTableSql(_tableSchema));

            if (await TableExistsAsync()) _logger.LogDebug("Таблица users успешно создана");

            else
            {
                _logger.LogError(
               "Таблица users не создана. " +
               $"SQL: {_schemaSqlGenerator.GenerateCreateTableSql(_tableSchema)}, " +
               $"Соединение: connectionState{_dbConnection.State}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка при создании таблицы users: {Error}", ex.Message);
            throw;
        }
    }

    private async Task CheckTableStructureAsync()
    {
        try
        {
            _logger.LogInformation("Проверка структуры таблицы ...");
            await using var conn = _dbConnection;
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }
            var actualSchema = _tableSchema;
            var expectedSchema = await _schemaProvider.GetActualTableSchemaAsync(_tableSchema.TableName);

            var actualAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(actualSchema)??
                throw new ArgumentNullException(nameof(actualSchema));

            var expectedAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(expectedSchema) ??
                throw new ArgumentNullException(nameof(expectedSchema));

            // Обрабатываем случай отсутствия схемы
            if (actualSchema is null)
            {
                _logger.LogError($"❌ Не удалось получить схему таблицы {expectedAdaptedSchema.TableName}");
                return;
            }

            // Сравниваем схемы
            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedAdaptedSchema, actualAdaptedSchema);

            // Выводим детализированные результаты
            if (diff.IsIdentical)
            {
                _logger.LogInformation("✅ Схемы совпадают.");
            }
            else
            {
                _logger.LogWarning("⚠️  Найденные расхождения:");

                foreach (var missing in diff.MissingColumns)
                    _logger.LogWarning($"   - Отсутствует колонка: {missing.Name}");

                foreach (var extra in diff.ExtraColumns)
                    _logger.LogWarning($"   - Лишняя колонка: {extra.Name}");

                foreach (var mismatch in diff.MismatchedColumns)
                    _logger.LogWarning(
                        $"   - Несовпадение в колонке '{mismatch.ColumnName}': " +
                        $"ожидалось {mismatch.Expected}, реально {mismatch.Actual}"
                    );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Ошибка проверки схемы:  ", ex.Message);            
        }
    }
}
