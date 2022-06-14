namespace Komodo.Utilities;

using System.Diagnostics;
using System.Text.RegularExpressions;

public record Position(int Line, int Column)
{
    public override string ToString() => $"{Line}:{Column}";
}

public record TextSpan(string SourceFileName, Position Start, int Length)
{
    private static readonly Regex REGEX_PATTERN = new Regex("(?<sfn>[^:]+):(?<line>[0-9]+):(?<column>[0-9]+)::(?<length>[0-9+])", RegexOptions.Compiled);
    
    public override string ToString() => $"{SourceFileName}:{Start}::{Length}";

    public static TextSpan From(string s)
    {
        Match m = REGEX_PATTERN.Match(s);

        if (!m.Success)
            throw new ArgumentException($"Invalid format: {s}");

        var sourceFileName = m.Groups["sfn"].Value;
        var line = int.Parse(m.Groups["line"].Value);
        var column = int.Parse(m.Groups["column"].Value);
        var length = int.Parse(m.Groups["length"].Value);

        return new TextSpan(sourceFileName, new Position(line, column), length);
    }
}

public record SourceFile
{
    public string Name { get; }
    public string Text { get; }

    private readonly int[] _lineStarts;

    public int Length => Text.Length;

    public SourceFile(string name, string text)
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

    public TextSpan GetSpan(int offset, int length) => new TextSpan(Name, GetPosition(offset), length);

    public static Result<SourceFile, string> Load(string path)
    {
        try { return new Result<SourceFile, string>.Success(new SourceFile(path, System.IO.File.ReadAllText(path))); }
        catch (Exception e) { return new Result<SourceFile, string>.Failure(e.Message); }
    }
}