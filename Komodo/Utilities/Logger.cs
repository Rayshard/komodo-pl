using System.Globalization;

namespace Komodo.Utilities;

public enum LogLevel { DEBUG, INFO, WARNING, ERROR, NOLOG }

public static class Logger
{
    public static LogLevel MinLevel { get; set; }

    private static void Log(LogLevel level, string log, bool startOnNewLine)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

        if (level < MinLevel)
            return;

        switch (level)
        {
            case LogLevel.DEBUG: Console.ForegroundColor = ConsoleColor.DarkCyan; break;
            case LogLevel.INFO: Console.ForegroundColor = ConsoleColor.DarkGray; break;
            case LogLevel.WARNING: Console.ForegroundColor = ConsoleColor.DarkYellow; break;
            case LogLevel.ERROR: Console.ForegroundColor = ConsoleColor.DarkRed; break;
            case LogLevel.NOLOG: return;
            default: throw new NotImplementedException(level.ToString());
        }

        if (startOnNewLine)
            log = Environment.NewLine + log;

        Console.Error.WriteLine($"{time} [{Enum.GetName(level)}] {log}");
        Console.ResetColor();
    }

    public static void Debug(string debug, bool startOnNewLine = false) => Log(LogLevel.DEBUG, debug, startOnNewLine);
    public static void Info(string info, bool startOnNewLine = false) => Log(LogLevel.INFO, info, startOnNewLine);
    public static void Warning(string warning, bool startOnNewLine = false) => Log(LogLevel.WARNING, warning, startOnNewLine);
    public static void Error(string error, bool startOnNewLine = false) => Log(LogLevel.ERROR, error, startOnNewLine);
}
