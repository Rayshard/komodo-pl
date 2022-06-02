namespace Komodo.Utilities
{
    class Location
    {
        public SourceFile SourceFile { get; }
        public Span Span { get; }

        public Location(SourceFile sourceFile, Span span)
        {
            SourceFile = sourceFile;
            Span = span;
        }

        public override string ToString() => $"{SourceFile.Path}:{Span}";
    }
}