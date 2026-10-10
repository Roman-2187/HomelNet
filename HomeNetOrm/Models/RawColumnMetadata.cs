namespace HomeNetOrm.Models
{

    /// <summary>
    /// Плоская модель для автоматического маппинга метаданных СУБД через Dapper.
    /// </summary>
    public class RawColumnMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;

        // ⚡ Ставим bool, так как мы скорректировали SQL под выдачу флагов 1/0 и true/false
        public bool IsNullable { get; set; }

        public string KeyType { get; set; } = string.Empty;
        public string ExtraInfo { get; set; } = string.Empty;

        // Пустой конструктор для Dapper — теперь он скомпилирует объект без единого писка!
        public RawColumnMetadata() { }
    }
}


