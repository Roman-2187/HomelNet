using System.IO;
using System.Linq;

namespace HomeNet.Infrastructure.Shared.Diagnostics
{
    public static class LogFormatter
    {
        public static string Format(string rawMessage, string memberName, string filePath, int lineNumber, bool isAutoLog = false)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string className = string.IsNullOrEmpty(filePath)
                ? "UnknownClass"
                : Path.GetFileNameWithoutExtension(filePath).Split('.').Last() ?? "Unknown";

            string detectedNamespace = "Core";
            if (!string.IsNullOrEmpty(filePath))
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    detectedNamespace = Path.GetFileName(directory) ?? "Core";
                }
            }

            className = className.Replace('_', ' ').Trim();
            memberName = memberName.Replace('_', ' ').Trim();

            // Если это автологгер-интерцептор, вшиваем твою плашку [АВТОЛОГ]
            string prefix = isAutoLog ? " [АВТОЛОГ]" : "";

            return $"[{timestamp}]{prefix} [{detectedNamespace} -> {className}.{memberName}:{lineNumber}] | {rawMessage}";
        }
    }
}
