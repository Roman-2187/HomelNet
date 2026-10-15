using System.Threading.Tasks;

namespace HomeNetCore.Interfaces
{
    public interface IApiClient
    {
        Task<bool> PushMessageAsync(object messageEntity);
        Task<bool> PushUserAsync(object userEntity);
    }
}
