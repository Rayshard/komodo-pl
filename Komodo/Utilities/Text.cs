namespace Komodo.Utilities;

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

public record Position(int Line, int Column)
{
    public override string ToString() => $"{Line}:{Column}";
}

public record TextLocation(string SourceName, int Start, int End)
{
    private static readonly Regex REGEX_PATTERN = new Regex("(?<sourceName>[^\\[]+)\\[(?<start>[0-9]+),(?<end>[0-9]+)\\]", RegexOptions.Compiled);

    public int Length => End - Start;

    public override string ToString() => $"{SourceName}[{Start},{End}]";

    public string ToTerminalLink(Dictionary<string, TextSource> sources)
    {
        var src = sources[SourceName];
        var (start, end) = (src.GetPosition(Start), src.GetPosition(End));
        return $"{SourceName}:{start}::{end}";
    }

    public static TextLocation From(string s)
    {
        Match m = REGEX_PATTERN.Match(s);

        if (!m.Success)
            throw new ArgumentException($"Invalid format: {s}");

        var sourceName = m.Groups["sourceName"].Value;
        var start = int.Parse(m.Groups["start"].Value);
        var end = int.Parse(m.Groups["end"].Value);

        return new TextLocation(sourceName, start, end);
    }
}

public record TextSource
{
    public string Name { get; }
    public string Text { get; }
    public ReadOnlyDictionary<int, (int Start, string Text)> Lines { get; }

    public int Length => Text.Length;

    public TextSource(string name, string text)
    {
        Name = name;
        Text = text;

        var lines = new Dictionary<int, (int, string)>();
        var offset = 0;

        foreach (var line in Text.Split('\n'))
        {
            lines.Add(lines.Count() + 1, (offset, line));
            offset += line.Length + 1;
        }

        Lines = new ReadOnlyDictionary<int, (int Start, string Text)>(lines);
    }

    public Position GetPosition(int offset)
    {
        Trace.Assert(offset >= 0 && offset <= Length, $"Specified offset {offset} is out of bounds: [0, {Length}]");

        var position = new Position(1, 1);

        for (int lineNumber = 1; lineNumber <= Lines.Count; lineNumber++)
        {
            var lineStart = Lines[lineNumber].Start;

            if (offset < lineStart)
                break;

            position = new Position(lineNumber, offset - lineStart + 1);
        }

        return position;
    }

    public TextLocation GetLocation(int offset, int length) => new TextLocation(Name, offset, offset + length);

    public static TextSource Load(string path) => new TextSource(path, System.IO.File.ReadAllText(path));
}

public class TextSourceReader
{
    public TextSource Source { get; }

    private int offset; //TODO: this should be a long
    public int Offset
    {
        get => offset;
        set => offset = Math.Min(Math.Max(value, 0), Source.Length);
    }

    public Position Position => Source.GetPosition(Offset);
    public bool EndOfStream => offset >= Source.Length;

    public TextSourceReader(TextSource source) => Source = source;

    public char Peek() => offset == Source.Length ? '\0' : Source.Text[offset];

    public char? PeekIf(Func<char, bool> predicate)
    {
        var peeked = Peek();
        return predicate(peeked) ? peeked : null;
    }

    public bool PeekIf(Func<char, bool> predicate, ref char result)
    {
        var character = PeekIf(predicate);

        if (!character.HasValue)
            return false;

        result = character.Value;
        return true;
    }

    public string PeekWhile(Func<char, bool> predicate, uint maxChars = uint.MaxValue)
    {
        int start = offset;
        string result = ReadWhile(predicate, maxChars);

        offset = start;
        return result;
    }

    public bool PeekWhile(Func<char, bool> predicate, out string result, uint maxChars = uint.MaxValue)
    {
        result = PeekWhile(predicate, maxChars);
        return result.Length != 0;
    }

    public char Read() => offset == Source.Length ? '\0' : Source.Text[offset++];
    public char? ReadIf(Func<char, bool> predicate) => predicate(Peek()) ? Read() : null;

    public bool ReadIf(Func<char, bool> predicate, ref char result)
    {
        var character = ReadIf(predicate);

        if (!character.HasValue)
            return false;

        result = character.Value;
        return true;
    }

    public bool ReadIf(char c)
    {
        if (Peek() == c)
        {
            Read();
            return true;
        }
        return false;
    }

    public string ReadWhile(Func<char, bool> predicate, uint maxChars = uint.MaxValue)
    {
        string result = "";

        while (!EndOfStream && predicate(Peek()))
            result += Read();

        return result;
    }

    public bool ReadWhile(Func<char, bool> predicate, out string result, uint maxChars = uint.MaxValue)
    {
        result = ReadWhile(predicate, maxChars);
        return result.Length != 0;
    }

    public char? SkipIfWhiteSpace() => ReadIf(Char.IsWhiteSpace);
    public string SkipWhileWhiteSpace() => ReadWhile(Char.IsWhiteSpace);

    public TextLocation GetLocation(int endpoint)
        => endpoint <= offset ? Source.GetLocation(endpoint, offset - endpoint) : Source.GetLocation(offset, endpoint - offset);
}