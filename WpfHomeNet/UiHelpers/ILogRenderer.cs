using HomeNetCore.Helpers;

namespace WpfHomeNet.UiHelpers
{
    public interface ILogRenderer
    {
        Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating);
    }
}