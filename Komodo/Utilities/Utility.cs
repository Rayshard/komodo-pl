using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Komodo.Utilities;

public static class Utility
{
    private static Regex HexCharRegex = new Regex("^[a-fA-F0-9]$");
    
    public static bool IsHex(this char c) => HexCharRegex.Match(c.ToString()).Success;

    public static string WithIndent(this string s, string indent = "    ") => string.Join('\n', s.Split('\n').Select(line => indent + line));

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

    public static JSchema EnumToJSchema<T>() where T : struct, Enum => JSchema.Parse($@"
        {{ 'enum': [{Stringify(Enum.GetNames<T>().Select(name => $"'{name}'"), ", ")}]}}
    ");

    public static void ValidateJSON(JToken json, JSchema schema)
    {
        if (!json.IsValid(schema, out IList<string> errorMessages))
        {
            foreach (var errorMessage in errorMessages)
                Logger.Error(errorMessage);

            throw new Exception("Invalid JSON");
        }
    }

    public static Regex Concat(IEnumerable<Regex> regexes) => new Regex(Stringify(regexes.Select(regex => $"({regex})"), "|"));
}
