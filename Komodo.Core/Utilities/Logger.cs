using System.Globalization;

namespace Komodo.Core.Utilities;

public enum LogLevel { DEBUG, INFO, WARNING, ERROR, NOLOG }

public static class Logger
{
    public static LogLevel MinLevel = LogLevel.ERROR;
    public static Action<LogLevel, string> Callback = (level, log) => Console.WriteLine(log);

    private static void Log(LogLevel level, object log, bool startOnNewLine)
    {
        if (level < MinLevel)
            return;

        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

        if (startOnNewLine)
            log = Environment.NewLine + log;

        Callback(level, $"{time} [{Enum.GetName(level)}] {log}");
    }

    public static void Debug(object debug, bool startOnNewLine = false) => Log(LogLevel.DEBUG, debug, startOnNewLine);
    public static void Info(object info, bool startOnNewLine = false) => Log(LogLevel.INFO, info, startOnNewLine);
    public static void Warning(object warning, bool startOnNewLine = false) => Log(LogLevel.WARNING, warning, startOnNewLine);
    public static void Error(object error, bool startOnNewLine = false) => Log(LogLevel.ERROR, error, startOnNewLine);
}
