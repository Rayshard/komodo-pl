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

        for(int lineNumber = 1; lineNumber <= Lines.Count; lineNumber++)
        {
            var lineStart = Lines[lineNumber].Start;

            if (offset < lineStart)
                break;

            position = new Position(lineNumber, offset - lineStart + 1);
        }

        return position;
    }

    public TextLocation GetLocation(int offset, int length) => new TextLocation(Name, offset, offset + length);

    public static Result<TextSource, string> Load(string path)
    {
        try { return new Result<TextSource, string>.Success(new TextSource(path, System.IO.File.ReadAllText(path))); }
        catch (Exception e) { return new Result<TextSource, string>.Failure(e.Message); }
    }
}