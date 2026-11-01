using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

public class ConsoleLogger : ILogger
{
    private readonly Dictionary<LogLevel, ConsoleColor> _colors;

    public ConsoleLogger(Dictionary<LogLevel, ConsoleColor>? customColors = null)
    {
        _colors = customColors ?? new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Info, ConsoleColor.Green },
            { LogLevel.Debug, ConsoleColor.Cyan },
            { LogLevel.Warning, ConsoleColor.Yellow },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.Magenta }
        };
    }

    public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0)
    {
        string fullLine = LogFormatter.Format(message, memberName, filePath, lineNumber, isAutoLog: false);

        Console.ForegroundColor = _colors.TryGetValue(level, out var color) ? color : ConsoleColor.White;
        Console.WriteLine(fullLine);
        Console.ResetColor();
    }
}
