namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IDeleteUserVm
    {
        public record Deleted(Guid Id);
    }
}
