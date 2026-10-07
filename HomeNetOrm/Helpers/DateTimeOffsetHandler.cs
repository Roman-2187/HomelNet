using Dapper;
using System;
using System.Data;

namespace HomeNetOrm.Helpers
{
    /// <summary>
    /// Специальный обработчик типов для Dapper: обучает его безболезненно парсить 
    /// любые длинные текстовые строки дат из SQLite в C# тип DateTimeOffset.
    /// </summary>
    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            // Сохраняем в СУБД в стандартизированном ISO-формате строки
            parameter.Value = value.ToString("o");
        }

        public override DateTimeOffset Parse(object value)
        {
            // Если из SQLite прилетела строка — парсим её стандартными средствами .NET,
            // которые, в отличие от Dapper, переваривают любую длину миллисекунд!
            if (value is string str && DateTimeOffset.TryParse(str, out var parsed))
            {
                return parsed;
            }

            if (value is DateTime dt)
            {
                return new DateTimeOffset(dt);
            }

            return DateTimeOffset.MinValue;
        }
    }
}
