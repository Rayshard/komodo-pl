using System.Globalization;

namespace Komodo.Utilities;

public enum LogLevel { DEBUG, INFO, WARNING, ERROR, NOLOG }

public static class Logger
{
    public static LogLevel MinLevel { get; set; }

    private static void Log(LogLevel level, string log)
    {
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

        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture)} [{Enum.GetName(level)}] {log}");
        Console.ResetColor();
    }

    public static void Debug(string debug) => Log(LogLevel.DEBUG, debug);
    public static void Info(string info) => Log(LogLevel.INFO, info);
    public static void Warning(string warning) => Log(LogLevel.WARNING, warning);
    public static void Error(string error) => Log(LogLevel.ERROR, error);
}
