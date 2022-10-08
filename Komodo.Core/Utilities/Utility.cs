using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.ObjectModel;

namespace Komodo.Core.Utilities;

public static class Utility
{
    private static readonly Regex HexCharRegex = new Regex("^[a-fA-F0-9]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsHex(this char c) => HexCharRegex.Match(c.ToString()).Success;

    public static string WithIndent(this string s, string indent = "    ", string delimiter = "\n") => string.Join(delimiter, s.Split(delimiter).Select(line => indent + line));

    public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> enumerable, bool condition, T value) => condition ? enumerable.Append(value) : enumerable;

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

    public static string Stringify<T>(this IEnumerable<T> items, string delimiter, (string Prefix, string Suffix) wrapper) => $"{wrapper.Prefix}{string.Join(delimiter, items)}{wrapper.Suffix}";
    public static string Stringify<T>(this IEnumerable<T> items, string delimiter) => Stringify(items, delimiter, ("", ""));
    public static string Stringify<T>(string delimiter, (string Prefix, string Suffix) wrapper) where T : struct, Enum => Stringify(Enum.GetValues<T>(), delimiter, wrapper);
    public static string Stringify<T>(string delimiter) where T : struct, Enum => Stringify(Enum.GetValues<T>(), delimiter);

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

    public static Regex Concat(IEnumerable<Regex> regexes) => new Regex(
        Stringify(regexes.Select(regex => $"({regex})"), "|"),
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static (TKey, TValue) AsTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> pair) => (pair.Key, pair.Value);
    public static KeyValuePair<TK, TV> GetEntry<TK, TV>(this IDictionary<TK, TV> d, TK k) => new KeyValuePair<TK, TV>(k, d[k]);

    public static bool IsEmpty<T>(this ICollection<T> collection) => collection.Count == 0;

    public static IEnumerable<(int Index, T Value)> WithIndices<T>(this IEnumerable<T> enumerable) => enumerable.Select((value, i) => (i, value));

    public static IEnumerable<TItem> AssertAllDistinct<TItem, TKey>(this IEnumerable<TItem> enumerable, Func<TItem, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();

        foreach (var item in enumerable)
        {
            var key = keySelector(item);

            if (seen.Contains(key)) { throw new Exception($"Duplicate key: {key}"); }
            else { seen.Add(key); }
        }

        return enumerable;
    }

    public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> enumerable) where K : notnull
    {
        var result = new Dictionary<K, V>();

        foreach (var item in enumerable)
            result.Add(item.Key, item.Value);

        return result;
    }

    public static (ReadOnlyCollection<TValue>, ReadOnlyDictionary<TKey, int>) ToCollectionWithMap<TItem, TKey, TValue>(this IEnumerable<TItem> enumerable, Func<TItem, TKey?> keySelector, Func<TItem, TValue> valueSelector) where TKey : notnull
    {
        var map = new Dictionary<TKey, int>();
        var list = new List<TValue>();

        foreach (var item in enumerable)
        {
            var key = keySelector(item);
            if (key is not null)
                map[key] = list.Count;

            list.Add(valueSelector(item));
        }

        return (new ReadOnlyCollection<TValue>(list.ToArray()), new ReadOnlyDictionary<TKey, int>(map));
    }

    public static (ReadOnlyCollection<TItem>, ReadOnlyDictionary<TKey, int>) ToArrayWithMap<TItem, TKey>(this IEnumerable<TItem> enumerable, Func<TItem, TKey?> keySelector) where TKey : notnull
        => enumerable.ToCollectionWithMap(keySelector, item => item);

    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }

    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable) => enumerable.SelectMany(x => x);

    public static int GetOrderDependentHashCode<T>(this IEnumerable<T> source)
    {
        unchecked
        {
            int hash = 19;

            foreach (var value in source)
                hash = hash * 31 + (value is null ? 617 : value.GetHashCode());

            return hash;
        }
    }

    /* From: https://stackoverflow.com/a/670068/19809812 */
    public static int GetOrderIndependentHashCode<T>(this IEnumerable<T> source) where T : notnull
    {
        int hash = 0;
        int curHash;
        int bitOffset = 0;
        var valueCounts = new Dictionary<T, int>(); // Stores number of occurences so far of each value.

        foreach (T element in source)
        {
            curHash = EqualityComparer<T>.Default.GetHashCode(element);

            if (valueCounts.TryGetValue(element, out bitOffset)) { valueCounts[element] = bitOffset + 1; }
            else { valueCounts.Add(element, bitOffset); }

            // The current hash code is shifted (with wrapping) one bit
            // further left on each successive recurrence of a certain
            // value to widen the distribution.
            // 37 is an arbitrary low prime number that helps the
            // algorithm to smooth out the distribution.
            hash = unchecked(hash + ((curHash << bitOffset) | (curHash >> (32 - bitOffset))) * 37);
        }

        return hash;
    }
}
