using Dapper;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeSocialNetwork.Core; // Подключаем пространство имен нашего SchemaRegistry
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using WpfHomeNet.Data.Schemes.CheckTableBd;

public class DBInitializer
{
    private readonly DbConnection _dbConnection;
    private readonly ISchemaSqlInitializer _schemaSqlGenerator;
    private readonly ILogger _logger;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ISchemaAdapter _schemaAdapter;

    // Конструктор стал чище — больше не нужно передавать сюда коллекцию схем!
    public DBInitializer(
        DbConnection connection,
        ISchemaProvider schemaProvider,
        ISchemaAdapter schemaAdapter,
        ISchemaSqlInitializer schemaSqlGenerator,
        ILogger logger)
    {
        _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
        _dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        _schemaSqlGenerator = schemaSqlGenerator ?? throw new ArgumentNullException(nameof(schemaSqlGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaAdapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("=== СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ ===");

        // 🧙 Magick: Прямо здесь берём все зарегистрированные таблицы из нашего реестра!
        var tableSchemas = SchemaRegistry.GetAllSchemas();

        foreach (var schema in tableSchemas)
        {
            try
            {
                _logger.LogInformation($"[БД] Проверка таблицы: {schema.TableName}...");

                if (!await TableExistsAsync(schema.TableName))
                {
                    await CreateTableAsync(schema);
                }
                else
                {
                    _logger.LogDebug($"[БД] Таблица {schema.TableName} существует, сверяю структуру...");
                    await CheckTableStructureAsync(schema);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[КРИТ] Сбой при обработке таблицы {schema.TableName}: {ex.Message}");
                throw; // Падаем, если база данных повреждена или заблокирована
            }
        }



        _logger.LogInformation("=== ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ЗАВЕРШЕНА ===");
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = _schemaSqlGenerator.GenerateTableExistsSql(tableName);
        try
        {
            var result = await _dbConnection.ExecuteScalarAsync<int>(sql, new { tableName });
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка проверки существования {tableName}: {ex.Message}");
            return false;
        }
    }

    private async Task CreateTableAsync(TableSchema schema)
    {
        _logger.LogWarning($"Таблица {schema.TableName} не обнаружена. Запуск генерации...");

        if (_dbConnection.State != ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }

        await _dbConnection.ExecuteAsync(_schemaSqlGenerator.GenerateCreateTableSql(schema));

        if (await TableExistsAsync(schema.TableName))
            _logger.LogInformation($"✅ Таблица {schema.TableName} успешно создана в БД.");
        else
        {
            _logger.LogError($"❌ Ошибка создания! Таблица {schema.TableName} отсутствует после выполнения скрипта.");
        }
    }

    private async Task CheckTableStructureAsync(TableSchema actualSchema)
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }

        var expectedSchema = await _schemaProvider.GetActualTableSchemaAsync(actualSchema.TableName);

        var actualAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(actualSchema) ??
            throw new ArgumentNullException(nameof(actualSchema));

        var expectedAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(expectedSchema) ??
            throw new ArgumentNullException(nameof(expectedSchema));

        var comparer = new SchemaComparer();
        var diff = comparer.Compare(expectedAdaptedSchema, actualAdaptedSchema);

        if (diff.IsIdentical)
        {
            _logger.LogInformation($"  -> Структура таблицы {actualSchema.TableName} в порядке.");
        }
        else
        {
            _logger.LogWarning($"⚠️ Обнаружены расхождения в структуру {actualSchema.TableName}:");

            foreach (var missing in diff.MissingColumns)
                _logger.LogWarning($"   [-] Отсутствует колонка: {missing.Name}");

            foreach (var extra in diff.ExtraColumns)
                _logger.LogWarning($"   [+] Обнаружена лишняя колонка: {extra.Name}");

            foreach (var mismatch in diff.MismatchedColumns)
                _logger.LogWarning($"   [*] Сдвиг типа в '{mismatch.ColumnName}': ожидалось {mismatch.Expected}, прилетело {mismatch.Actual}");
        }
    }
}
