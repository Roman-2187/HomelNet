using Dapper;
using System.Data;
using System.Data.Common;
using WpfHomeNet.Data.Generators;

using WpfHomeNet.Data.Schemes;
using WpfHomeNet.Data.TableUserBDs;
using WpfHomeNet.Helpers;
public class DBInitializer
{
    IDbConnection _dbConnection;
    ISqlQueryGenerator _sqlQueryGenerator;
    private readonly ILogger _logger;
    ISchemaProvider _schemaProvider;
    TableSchema _tableSchema;


    public DBInitializer
        (
        ISchemaProvider schemaProvider,
        IDbConnection connection,
        ISqlQueryGenerator sqlQueryGenerator,
        TableSchema tableSchema,
        ILogger logger
        )
    {
        _dbConnection = connection;
        _tableSchema = tableSchema;
        _sqlQueryGenerator = sqlQueryGenerator;
        _logger = logger;
        _schemaProvider = schemaProvider;

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
        if (_dbConnection.State != ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }

        return await _dbConnection.ExecuteScalarAsync<int>(
            _sqlQueryGenerator.GenerateTableExistsSql(),
            new { tableName = _tableSchema.TableName } ) > 0;
       
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
            await _dbConnection.ExecuteAsync(_sqlQueryGenerator.GenerateCreateTableSql());

            if (await TableExistsAsync()) _logger.LogDebug("Таблица users успешно создана.");

            else
            {
                _logger.LogError(
               "Таблица users не создана. " +
               $"SQL: {_sqlQueryGenerator.GenerateCreateTableSql()}, " +
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
            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }
              
                              
            if (!await TableExistsAsync())
            {
                Console.WriteLine($"❌ Таблица {expectedSchema.TableName} не существует!");
                return;
            }


            // 2. Получаем фактическую схему из БД
            List<ColumnSchema> columnSchemas = await _schemaProvider.GetActualColumnsAsync(
                (DbConnection)_dbConnection,
                expectedSchema.TableName);
            var actualColumns = columnSchemas;

            var actualSchema = new TableSchema
            {
                TableName = expectedSchema.TableName,
                Columns = actualColumns
            };

            // 3. Сравниваем схемы
            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedSchema, actualSchema);

            // 4. Обрабатываем результаты
            if (diff.IsIdentical)
            {
                Console.WriteLine("✅ Схема таблицы соответствует ожидаемой!");
            }
            else
            {
                if (diff.MissingColumns.Any())
                    Console.WriteLine($"❌ Отсутствуют колонки: {string.Join(", ", diff.MissingColumns.Select(c => c.Name))}");

                if (diff.ExtraColumns.Any())
                    Console.WriteLine($"⚠️ Лишние колонки: {string.Join(", ", diff.ExtraColumns.Select(c => c.Name))}");

                if (diff.MismatchedColumns.Any())
                {
                    foreach (var mismatch in diff.MismatchedColumns)
                    {
                        Console.WriteLine($"🔍 Несоответствие в колонке '{mismatch.ColumnName}':");
                        Console.WriteLine($"   Ожидалось: {DescribeColumn(mismatch.Expected)}");
                        Console.WriteLine($"   Фактически: {DescribeColumn(mismatch.Actual)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка проверки схемы: {ex.Message}");
        }
        finally
        {
            _dbConnection.Close();
        }
    }





   







    // Вспомогательный метод для описания колонки
    private string DescribeColumn(ColumnSchema column)
    {
        return $"{column.Type}({column.Length ?? 0}) " +
               $"NULL: {column.IsNullable} " +
               $"PK: {column.IsPrimaryKey} " +
               $"AI: {column.IsAutoIncrement}";
    }





}

