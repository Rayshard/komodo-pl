namespace Komodo.Utilities;

public record Location(SourceFile SourceFile, Span Span)
{
    public override string ToString() => $"{SourceFile.Name}:{Span}";
}