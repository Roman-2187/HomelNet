using HomeNetCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IUsersTableVm
    {
       
        public record Added(UserEntity User);
        public record Refreshed(IReadOnlyList<UserEntity> Users);
        public record RefreshRequest();
    }
}
