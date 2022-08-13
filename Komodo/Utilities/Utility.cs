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
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace
        };

        using var writer = XmlWriter.Create(sb, settings);
            document.Save(writer);
            
        return sb.ToString();
    }

    public static string StringifyEnumerable<T>(string prefix, IEnumerable<T> items, string suffix, string delimiter) => $"{prefix}{string.Join(delimiter, items)}{suffix}";
}
