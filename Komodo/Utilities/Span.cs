namespace Komodo.Utilities;

public record Span(Position Start, Position End)
{
    public override string ToString() => $"{Start}::{End}";
}
