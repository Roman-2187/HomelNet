using WpfHomeNet.Data.Bilders;
using WpfHomeNet.Data.Schemes;

namespace WpfHomeNet.Data.Generators
{
    public class TableGenerator
    {
        private readonly string _tableName;
        private readonly List<ColumnBuilder> _columnBuilders = new();  // Храним builders, а не schemas!

        public TableGenerator(string tableName) => _tableName = tableName;

        // Возвращаем builder для дальнейшей настройки
        public ColumnBuilder AddColumn(string name)
        {
            var builder = new ColumnBuilder(name);
            _columnBuilders.Add(builder);
            return builder;
        }

        // Финализируем все builders при генерации схемы
        public TableSchema Generate() => new TableSchema
        {
            TableName = _tableName,
            Columns = _columnBuilders
                .Select(builder => builder.Build())  // Build() вызывается здесь!
                .ToList()
        };
    }








}
