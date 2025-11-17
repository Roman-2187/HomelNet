using WpfHomeNet.Data.TableUserBDs;

namespace WpfHomeNet.Data.Generators
{
    public interface ISqlQueryGenerator
    {
        public string GenerateCreateTableSql();
        public string GenerateInsertSql();
        public string GenerateSelectByIdSql();
        public string GenerateSelectByEmailSql();
        public string GenerateUpdateSql();
        public string GenerateDeleteSql();
        public string GenerateTableExistsSql();       
        public string GenerateSelectAllSql();
    }




   




}
