using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

public class AggregateLogger : ILogger
{
    private readonly List<ILogger> _loggers = new();

    public AggregateLogger(params ILogger[] loggers)
    {
        if (loggers != null) _loggers.AddRange(loggers);
    }

    public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(level, message, memberName, filePath, lineNumber);
        }
    }
}
