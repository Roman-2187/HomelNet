using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Schemes;

namespace HomeNetCore.Models
{
    public class UsersTable : TableSchemaBuilder
    {
        public UsersTable(): base( "users")
            
        {                            
                 AddColumn("Id").AsInteger().PrimaryKey().AutoIncrement();
                    
                 AddColumn("FirstName").AsVarchar(50).DisallowNull();    
                    
                 AddColumn("LastName").AsVarchar(50).AllowNull(); // NULL разрешён
                    
                 AddColumn("PhoneNumber").AsVarchar(50).AllowNull();  // NULL разрешён   
                   
                 AddColumn("Email").AsVarchar(50).DisallowNull().Unique();
               
                 AddColumn("Password").AsVarchar(50).DisallowNull();               
                    
                 AddColumn("CreatedAt").CreatedAt().DisallowNull();                             
        }

        // Явный метод для получения схемы
        public TableSchema Build() => Generate();
    }



}
 