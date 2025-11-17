using WpfHomeNet.Data.Schemes;

namespace WpfHomeNet.Data.Generators
{
    public class UsersTableGenerator : TableGenerator
    {
        public UsersTableGenerator(): base( "users")
            
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
