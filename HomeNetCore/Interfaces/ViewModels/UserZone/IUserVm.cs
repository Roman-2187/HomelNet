using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums.Navigation;

namespace HomeNetCore.Interfaces.ViewModels.UserZone
{
    public interface IUserVm
    {


        ClientSubTab ActiveClientTab { get; }

        public record OpenAuth;          
        public record OpenRegistration;  
        public record BackToStart;      
      
       
      
        public record LogoutClient;
 
        public record ClientTabChanged(ClientSubTab ActiveTab);


     
        public record UserSignedIn(UserEntity User);

      
        public record UserSignedUp(UserEntity User);

    }
}
