using WpfHomeNet.Data.Schemes;

namespace WpfHomeNet.Data.Generators
{
    public class UsersTableGenerator : TableGenerator
    {
        public UsersTableGenerator(): base( "users")
            
        {                            
                 AddColumn("Id").AsInteger().PrimaryKey().AutoIncrement();
                    
                 AddColumn("FirstName").AsVarchar(50).NotNull();    
                    
                 AddColumn("LastName").AsVarchar(50); // NULL разрешён
                    
                 AddColumn("PhoneNumber").AsVarchar(50);  // NULL разрешён   
                   
                 AddColumn("Email").AsVarchar(50).NotNull().Unique();
               
                 AddColumn("Password").AsVarchar(50).NotNull();               
                    
                 AddColumn("CreatedAt").CreatedAt().NotNull();                             
        }

        // Явный метод для получения схемы
        public TableSchema Build() => Generate();
    }



}
