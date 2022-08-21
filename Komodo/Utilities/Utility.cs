using System.Text;
using System.Text.Json;
using System.Xml;

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

    public static string ToFormattedString(XmlDocument document)
    {
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace
        };

        using MemoryStream ms = new MemoryStream();
        using XmlWriter writer = XmlWriter.Create(ms, settings);

        document.Save(writer);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static string Stringify<T>(IEnumerable<T> items, string delimiter, (string Prefix, string Suffix) wrapper) => $"{wrapper.Prefix}{string.Join(delimiter, items)}{wrapper.Suffix}";
    public static string Stringify<T>(IEnumerable<T> items, string delimiter) => Stringify(items, delimiter, ("", ""));
}
