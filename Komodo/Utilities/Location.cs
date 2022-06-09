using System.Text.RegularExpressions;

namespace Komodo.Utilities;

public record Location(string SourceFileName, Span Span)
{
    private static readonly Regex REGEX_PATTERN = new Regex("(?<sf>[^:]+):(?<l1>[0-9]+):(?<c1>[0-9]+)::(?<l2>[0-9+]):(?<c2>[0-9]+)", RegexOptions.Compiled);

    public override string ToString() => $"{SourceFileName}:{Span}";

    public static Location From(string s)
    {
        Match m = REGEX_PATTERN.Match(s);

        if (!m.Success)
            throw new ArgumentException($"Invalid format: {s}");

        var sourceFileName = m.Groups["sf"].Value;
        var startLine = int.Parse(m.Groups["l1"].Value);
        var startColumn = int.Parse(m.Groups["c1"].Value);
        var endLine = int.Parse(m.Groups["l2"].Value);
        var endColumn = int.Parse(m.Groups["c2"].Value);

        return new Location(sourceFileName, new Span(new Position(startLine, startColumn), new Position(endLine, endColumn)));
    }
}