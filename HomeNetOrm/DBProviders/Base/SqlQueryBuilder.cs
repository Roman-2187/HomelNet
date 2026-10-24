using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🛠️ Статический класс-обслуживатель СУБД.
    /// Штампует любые строковые SQL-блоки для генераторов и инициализаторов без лишней каши.
    /// Доверяет регистру пришедших из "турникета" схем TableSchema.
    /// </summary>
    internal static class SqlQueryBuilder
    {
        // --- ОБСЛУЖИВАНИЕ ИНИЦИАЛИЗАТОРОВ (Сборка структуры БД) ---

        /// <summary>
        /// Берет готовые дефиниции колонок из статического адаптера и склеивает их через запятую для CREATE TABLE.
        /// </summary>
        public static string BuildCreateTableColumns(IDbProviderSpecification spec, TableSchema schema)
        {
            // Передаем схему в адаптер, который сформирует дефиниции на основе ее текущего регистра
            List<string> definitions = GenericSchemaAdapter.GetColumnDefinitions(spec, schema);
            return string.Join(", ", definitions);
        }

        /// <summary>
        /// Склеивает колонки составного первичного ключа через запятую для SQL: "id", "device_token"
        /// </summary>
        public static string BuildCompositePrimaryKeyFields(TableSchema snakeSchema)
        {
            var pkColumns = snakeSchema.Columns.Where(c => c.IsPrimaryKey);
            return string.Join(", ", pkColumns.Select(c => $"\"{(c.Name ?? throw new InvalidOperationException("Имя колонки пустое"))}\""));
        }


        // --- ОБСЛУЖИВАНИЕ ГЕНЕРАТОРОВ (Запросы Dapper) ---

        /// <summary>
        /// Выборка полей с маппингом (SELECT "first_name" AS FirstName, "email" AS Email)
        /// </summary>
        public static string BuildSelectFields(TableSchema snakeSchema, TableSchema camelSchema)
        {
            if (snakeSchema.Columns.Count != camelSchema.Columns.Count)
                throw new InvalidOperationException("Асимметрия колонок в переданных схемах для BuildSelectFields");

            return string.Join(", ", snakeSchema.Columns.Select((col, index) =>
                $"\"{col.Name}\" AS {camelSchema.Columns[index].Name}"));
        }

        /// <summary>
        /// Физические поля для INSERT ("first_name", "last_name")
        /// </summary>
        public static string BuildInsertFields(TableSchema snakeSchema)
        {
            var insertable = snakeSchema.Columns.Where(c => !c.IsAutoIncrement);
            return string.Join(", ", insertable.Select(c => $"\"{c.Name}\""));
        }

        /// <summary>
        /// Параметры Dapper для INSERT (@FirstName, @LastName)
        /// </summary>
        public static string BuildInsertParameters(TableSchema camelSchema)
        {
            var insertable = camelSchema.Columns.Where(c => !c.IsAutoIncrement);
            return string.Join(", ", insertable.Select(c => $"@{c.Name}"));
        }

        /// <summary>
        /// Блок SET для UPDATE ("first_name" = @FirstName, "email" = @Email)
        /// </summary>
        public static string BuildSetClause(TableSchema snakeSchema, TableSchema camelSchema)
        {
            if (snakeSchema.Columns.Count != camelSchema.Columns.Count)
                throw new InvalidOperationException("Асимметрия колонок в переданных схемах для BuildSetClause");

            var nonPkSnake = snakeSchema.Columns.Select((c, i) => new { Col = c, Index = i }).Where(x => !x.Col.IsPrimaryKey).ToList();
            var nonPkCamel = camelSchema.Columns.Select((c, i) => new { Col = c, Index = i }).Where(x => !x.Col.IsPrimaryKey).ToList();

            if (nonPkSnake.Count != nonPkCamel.Count)
                throw new InvalidOperationException("Асимметрия ключевых/неключевых колонок при формировании блока UPDATE");

            return string.Join(", ", nonPkSnake.Select((item, idx) =>
                $"\"{item.Col.Name}\" = @{nonPkCamel[idx].Col.Name}"));
        }
    }
}
