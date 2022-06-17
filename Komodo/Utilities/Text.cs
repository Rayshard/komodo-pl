namespace Komodo.Utilities;

using System.Diagnostics;
using System.Text.RegularExpressions;

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

    private readonly int[] _lineStarts;

    public int Length => Text.Length;

    public TextSource(string name, string text)
    {
        Name = name;
        Text = text;

        var lines = Text.Split('\n');
        _lineStarts = new int[lines.Length + 1];
        _lineStarts[0] = 0;

        for (int i = 0; i < lines.Length; i++)
            _lineStarts[i + 1] = _lineStarts[i] + lines[i].Length + 1;
    }

    public Position GetPosition(int offset)
    {
        Trace.Assert(offset >= 0 && offset <= Length, $"Specified offset {offset} is out of bounds: [0, {Length}]");

        var position = new Position(1, 1);

        for (int line = 0; line < _lineStarts.Length; line++)
        {
            var lineStart = _lineStarts[line];

            if (offset < lineStart)
                break;


            position = new Position(line + 1, offset - lineStart + 1);
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