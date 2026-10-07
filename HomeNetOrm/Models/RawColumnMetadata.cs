namespace HomeNetOrm.Models
{
    /// <summary>
    /// Строгий, легковесный рекорд сырых метаданных колонки из БД. Чистый переносчик данных (DTO).
    /// </summary>
    public record RawColumnMetadata(
        string Name,
        string DataType,
        bool IsNullable,
        string KeyType,
        string ExtraInfo
    );
}
