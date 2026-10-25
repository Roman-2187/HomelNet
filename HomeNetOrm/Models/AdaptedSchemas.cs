namespace HomeNetOrm.Models
{
    internal class AdaptedSchemas
    {
        // Компилятор не даст скомпилировать код, если при создании забыли указать Camel или Snake
        public required TableSchema Camel { get; init; }
        public required TableSchema Snake { get; init; }
    }
}
