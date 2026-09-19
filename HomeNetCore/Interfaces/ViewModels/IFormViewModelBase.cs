namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IFormViewModelBase
    {
        public record VisibilityChanged(Type FormType, bool IsVisible);
    }
}
