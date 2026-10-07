using System;
using System.Data.Common;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Extensions
{
    /// <summary>
    /// Полноценный статический движок парсинга: расширяет возможности DbDataReader 
    /// для сборки метаданных под нужды GenericSchemaProvider.
    /// </summary>
    public static class ProviderExtensions
    {
        /// <summary>
        /// Выковыривает и собирает рекорд RawColumnMetadata из текущей строки ридера на основе спецификации.
        /// </summary>
        public static RawColumnMetadata ReadColumnMetadata(this DbDataReader reader, DbProviderSpecificationExtensions spec)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return new RawColumnMetadata(
                Name: reader.IsDBNull(spec.NameIndex) ? string.Empty : reader.GetString(spec.NameIndex),
                DataType: reader.IsDBNull(spec.TypeIndex) ? string.Empty : reader.GetString(spec.TypeIndex),
                IsNullable: ParseNullableField(reader, spec.NullableIndex),
                KeyType: ReadStringUniversal(reader, spec.PrimaryKeyIndex),
                ExtraInfo: spec.ExtraInfoIndex >= 0 && !reader.IsDBNull(spec.ExtraInfoIndex)
                    ? reader.GetValue(spec.ExtraInfoIndex)?.ToString() ?? string.Empty
                    : string.Empty
            );
        }

        #region 🛠 ВНУТРЕННИЕ МЕТОДЫ РАСПАКОВКИ ТИПОВ СУБД 🧼

        private static bool ParseNullableField(DbDataReader reader, int index)
        {
            if (index < 0 || reader.IsDBNull(index)) return false;

            string val = reader.GetValue(index)?.ToString() ?? string.Empty;

            if (val.Equals("YES", StringComparison.OrdinalIgnoreCase)) return true;
            if (val.Equals("NO", StringComparison.OrdinalIgnoreCase)) return false;

            if (bool.TryParse(val, out bool res)) return !res;

            return val.Equals("0");
        }

        private static string ReadStringUniversal(DbDataReader reader, int index)
        {
            if (index < 0 || reader.IsDBNull(index)) return string.Empty;
            return reader.GetValue(index).ToString() ?? string.Empty;
        }

        #endregion
    }
}
