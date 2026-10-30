using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Postgres
{
    internal static class SchemaRepositoryPostgresExtensions
    {
        /// <summary>
        /// Автоматический накат триггеров и функций версионирования для PostgreSQL.
        /// Красиво вынесен из основного движка прямо в слой работы со схемами! 🦾
        /// </summary>
        public static async Task MigratePostgresVersionTriggersAsync(
            this SchemaRepository _, // Магия расширения вешается на "табле криатер"
            DbConnection connection,
            ISchemaSqlGenerator schemaSqlInitializer,
            ISqlDialectTranslator translator,
            IEnumerable<TableSchema> tableSchemas,
            ILogger logger)
        {
            logger.LogInfo("[Postgres Migrator] Серверный режим: Проверка системы версионирования Postgres...");

            try
            {
                // 1. Создаем/обновляем глобальную функцию инкремента (используя твой метод расширения из PostgresSchemaExtensions)
                string functionSql = schemaSqlInitializer.GenerateVersionFunctionSql(translator);
                using (var cmdFunc = connection.CreateCommand())
                {
                    cmdFunc.CommandText = functionSql;
                    await cmdFunc.ExecuteNonQueryAsync();
                }
                logger.LogInfo("[Postgres Migrator] ✅ Глобальная функция инкремента 'update_version_func' проверена.");

                // 📋 Черный список таблиц, которым не нужно серверное версионирование
                var ignoreTablesForVersioning = new HashSet<string> { "device_sync", "devicesync" };

                // 2. Бежим по всем зарегистрированным таблицам и проверяем триггеры в системном каталоге pg_trigger
                foreach (var schema in tableSchemas)
                {
                    if (schema == null || string.IsNullOrEmpty(schema.TableName)) continue;

                    string cleanTableName = schema.TableName.Replace("\"", "").ToLower();

                    // Проверяем исключения
                    if (ignoreTablesForVersioning.Contains(cleanTableName))
                    {
                        logger.LogInfo($"[Postgres Migrator] Игнор-лист: Таблица '{schema.TableName}' исключена из серверного версионирования.");
                        continue;
                    }

                    string triggerName = $"trg_{cleanTableName}_version";

                    // Ищем существующий триггер в базе
                    bool triggerExists = false;
                    using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT COUNT(*) FROM pg_trigger WHERE tgname = @tgname;";
                        var param = checkCmd.CreateParameter();
                        param.ParameterName = "@tgname";
                        param.Value = triggerName;
                        checkCmd.Parameters.Add(param);

                        var result = await checkCmd.ExecuteScalarAsync();
                        triggerExists = Convert.ToInt32(result) > 0;
                    }

                    if (triggerExists)
                    {
                        logger.LogInfo($"[Postgres Migrator] Лог: Триггер версионирования '{triggerName}' уже существует для таблицы '{schema.TableName}'. Пропуск.");
                    }
                    else
                    {
                        logger.LogWarning($"[Postgres Migrator] Лог: Триггер '{triggerName}' НЕ найден. Генерируем и накатываем...");

                        // Вызываем твое Fluent-расширение генератора SQL триггера!
                        string triggerSql = schemaSqlInitializer.GenerateVersionTriggerSql(schema, translator);

                        using (var runCmd = connection.CreateCommand())
                        {
                            runCmd.CommandText = triggerSql;
                            await runCmd.ExecuteNonQueryAsync();
                        }
                        logger.LogInfo($"[Postgres Migrator] ✅ Триггер '{triggerName}' успешно привязан к таблице '{schema.TableName}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical($"[Postgres Migrator] ❌ КРИТИЧЕСКАЯ ОШИБКА инициализации триггеров версионирования: {ex.Message}");
                throw;
            }
        }
    }
}
