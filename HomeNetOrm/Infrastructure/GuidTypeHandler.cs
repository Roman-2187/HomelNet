using Dapper;
using System;

namespace HomeNetOrm.Infrastructure
{
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }

        public override Guid Parse(object value)
        {
            if (value is Guid guid) return guid;
            if (value is string str && Guid.TryParse(str, out var parsedGuid))
            {
                return parsedGuid;
            }
            return Guid.Empty;
        }
    }
}
