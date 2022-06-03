namespace Komodo.Utilities
{
    public struct Span
    {
        public Position Start { get; set; }
        public Position End { get; set; }

        public Span(Position start, Position end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start}::{End}";
    }
}