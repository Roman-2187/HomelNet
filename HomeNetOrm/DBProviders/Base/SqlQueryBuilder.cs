using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🛠️ Статический класс-обслуживатель СУБД.
    /// Штампует любые строковые SQL-блоки для генераторов и инициализаторов без лишней каши.
    /// </summary>
    internal static class SqlQueryBuilder
    {
        // --- ОБСЛУЖИВАНИЕ ИНИЦИАЛИЗАТОРОВ (Убираем джойны из CREATE TABLE) ---

        /// <summary>
        /// Берет готовые дефиниции колонок из статического адаптера и склеивает их через запятую для CREATE TABLE.
        /// </summary>
        public static string BuildCreateTableColumns(IDbProviderSpecification spec, TableSchema schema)
        {
            // 🔥 ФИКС: Вызываем GetColumnDefinitions напрямую через наш статический адаптер
            List<string> definitions = GenericSchemaAdapter.GetColumnDefinitions(spec, schema);
            return string.Join(", ", definitions);
        }

        /// <summary>
        /// Склеивает колонки составного первичного ключа через запятую: "id", "device_token"
        /// </summary>
        public static string BuildCompositePrimaryKeyFields(TableSchema schema, NameFormat format)
        {
            var pkColumns = schema.Columns.Where(c => c.IsPrimaryKey);
            // 🔥 ФИКС: ConvertColumnName теперь метод расширения для строк! Вызываем лаконично через точку
            return string.Join(", ", pkColumns.Select(c => $"\"{c.Name.ConvertColumnName(format)}\""));
        }


        // --- ОБСЛУЖИВАНИЕ ГЕНЕРАТОРОВ (Запросы Dapper) ---

        // Выборка (SELECT "first_name" AS FirstName)
        public static string BuildSelectFields(TableSchema schema, NameFormat format)
        {
            return string.Join(", ", schema.Columns.Select(c =>
            {
                // 🔥 ФИКС: Используем красивый метод расширения для имени колонки
                string dbName = c.Name.ConvertColumnName(format);
                return $"\"{dbName}\" AS {c.Name}";
            }));
        }

        // Поля для INSERT ("first_name", "last_name")
        public static string BuildInsertFields(TableSchema schema, NameFormat format)
        {
            var insertable = schema.Columns.Where(c => !c.IsAutoIncrement);
            // 🔥 ФИКС: И здесь бьем через расширение строки напрямую
            return string.Join(", ", insertable.Select(c => $"\"{c.Name.ConvertColumnName(format)}\""));
        }

        // Параметры для INSERT (@FirstName, @LastName)
        public static string BuildInsertParameters(TableSchema schema)
        {
            var insertable = schema.Columns.Where(c => !c.IsAutoIncrement);
            return string.Join(", ", insertable.Select(c => $"@{c.Name}"));
        }

        // Блок SET для UPDATE ("first_name" = @FirstName)
        public static string BuildSetClause(TableSchema schema, NameFormat format)
        {
            var nonPkColumns = schema.Columns.Where(c => !c.IsPrimaryKey);
            // 🔥 ФИКС: Вырезали прослойку адаптера из аргументов
            return string.Join(", ", nonPkColumns.Select(c =>
                $"\"{c.Name.ConvertColumnName(format)}\" = @{c.Name}"));
        }
    }
}
