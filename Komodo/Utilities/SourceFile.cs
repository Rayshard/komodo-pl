namespace Komodo.Utilities;

using System.Diagnostics;

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

    public Location GetLocation(int start, int end) => new Location(Name, new Span(GetPosition(start), GetPosition(end)));

    public static Result<SourceFile, string> Load(string path)
    {
        try { return new Result<SourceFile, string>.Success(new SourceFile(path, System.IO.File.ReadAllText(path))); }
        catch (Exception e) { return new Result<SourceFile, string>.Failure(e.Message); }
    }
}