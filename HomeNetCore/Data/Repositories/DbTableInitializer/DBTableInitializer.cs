using Dapper;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using HomeNetCore.Helpers;
using System.Data;
using System.Data.Common;
using WpfHomeNet.Data.Schemes.CheckTableBd;
public class DBTableInitializer
{
    private readonly DbConnection _dbConnection;
    private readonly ISchemaSqlInitializer _schemaSqlGenerator;
    private readonly ILogger _logger;
    private readonly GetSchemaProvider _schemaProvider;
    private readonly TableSchema _tableSchema;


    public DBTableInitializer
        (
        GetSchemaProvider schemaProvider,
        DbConnection connection,
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
    }

    public async Task InitializeAsync()
    {
        _logger.LogDebug("Инициализация БД: проверка таблицы users...");

        if (!await TableExistsAsync())
        {
            await CreateTableAsync();
        }

        else
        {
            await CheckTableStructureAsync();
        }
    }

    private async Task<bool> TableExistsAsync()
    {
        var query = _schemaSqlGenerator.GenerateTableExistsSql(_tableSchema.TableName);
        _logger.LogDebug($"Выполняемый запрос: {query}");

        try
        {
            var result = await _dbConnection.ExecuteScalarAsync<int>(
                query,
                new { tableName = _tableSchema.TableName }
            );

            _logger.LogDebug($"Результат запроса: {result}");
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
        _logger.LogWarning($"Таблица {_tableSchema.TableName} не найдена. Создаю новую...");
        try
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }
            await _dbConnection.ExecuteAsync(_schemaSqlGenerator.GenerateCreateTableSql(_tableSchema));

            if (await TableExistsAsync()) _logger.LogDebug("Таблица users успешно создана.");

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
        var expectedSchema = _tableSchema;

        try
        {            
            await using var conn = _dbConnection;
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            // Получаем фактическую схему
            var actualSchema = await _schemaProvider.GetActualTableSchemaAsync(expectedSchema.TableName);

            // Обрабатываем случай отсутствия схемы
            if (actualSchema is null)
            {
                _logger.LogError($"❌ Не удалось получить схему таблицы {expectedSchema.TableName}");
                return;
            }

            // Сравниваем схемы
            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedSchema, actualSchema);

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
            _logger.LogError("❌ Ошибка проверки схемы:  ",ex.Message);
                
                
            
        }
    }


}
