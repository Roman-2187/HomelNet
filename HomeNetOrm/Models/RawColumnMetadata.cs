namespace HomeNetOrm.Models
{
    // 🔥 ТЕПЕРЬ ОНА ЖИВЕТ ТУТ: Лёгкая структура-рекорд метаданных
    public readonly record struct RawColumnMetadata(
        string Name,
        string DataType,
        bool IsNullable,
        string KeyType,
        string ExtraInfo
    );
}
