using HomeNetCore.Data.Schemes;

namespace HomeNetCore.Data.Interfaces
{
    public interface ISchemaUserSqlGenerator
    {
        
      public  string GenerateInsert();
      public string GenerateUpdate();
      public string GenerateDelete();
      public string GenerateSelectById();
      public string GenerateSelectByEmail();
      public string GenerateSelectAll();
        string GenerateEmailExists();
    }
}
