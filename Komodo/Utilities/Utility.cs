using System.Text;
using System.Text.Json;

namespace Komodo.Utilities;

public static class Utility
{
    public static string PrettyPrintJSON(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true });
        document.WriteTo(writer);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static void PrintInfo(string info)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[INFO] {info}");
        Console.ResetColor();
    }

    public static string StringifyEnumerable<T>(string prefix, IEnumerable<T> items, string suffix, string delimiter) => $"{prefix}{string.Join(delimiter, items)}{suffix}";
}
