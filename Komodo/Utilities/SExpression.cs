using System.Globalization;
using System.Text.RegularExpressions;

namespace Komodo.Utilities;

public abstract record SExpression
{
    public abstract record Atom(string Value) : SExpression
    {
        public record Quoted(string Value) : Atom(Value)
        {
            public override string ToString()
            {
                string result = Value
                    .Replace("\\", "\\\\")  // Escape backslashes
                    .Replace("\"", "\\\"")  // Escape quotes
                    .Replace("\n", "\\n")   // Escape new lines
                    .Replace("\r", "\\r")   // Escape tabs
                    .Replace("\t", "\\t")   // Escape tabs
                    .Replace("\b", "\\b")   // Escape tabs
                    .Replace("\v", "\\v")   // Escape tabs
                    .Replace("\0", "\\0");  // Escape tabs

                return $"\"{result}\"";
            }
        }

        public record Unquoted(string Value) : Atom(VerifyValue(Value))
        {
            public static readonly Regex Regex = new Regex("^[^\\s\"\\(\\),]+$");

            public override string ToString() => Value;

            private static string VerifyValue(string value)
                => Regex.IsMatch(value) ? value : throw new InvalidOperationException($"Invalid unquoted value: {value}");
        }
    }

    public record List(IEnumerable<SExpression> Items) : SExpression
    {
        public List() : this(new SExpression[] { }) { }

        public override string ToString() => Utility.Stringify(Items, " ", ("(", ")"));
    }

    public static class Parser
    {
        public class ParseException : Exception
        {
            public TextLocation Location { get; }

            public ParseException(string message, TextLocation location) : base(message) => Location = location;
        }

        public record Node(SExpression Value, TextLocation Location) : SExpression
        {
            public override string ToString() => Value.ToString();
        }

        public static Node ParseList(TextSourceReader stream)
        {
            int start = stream.Offset;

            if (stream.Read() != '(')
                throw new ParseException("Lists must start with '('", stream.GetLocation(start));

            stream.SkipWhileWhiteSpace();

            var items = new System.Collections.Generic.List<SExpression>();

            while (true)
            {
                switch (stream.Peek())
                {
                    case ')':
                        {
                            stream.Read();
                            return new Node(new List(items), stream.GetLocation(start));
                        }
                    case '\0': throw new ParseException("Expected ')' but found EOF", stream.GetLocation(stream.Offset));
                    default: items.Add(Parse(stream)); break;
                }

                if (!stream.PeekIf(c => Char.IsWhiteSpace(c) || c == ')').HasValue)
                    throw new ParseException("Expected whitespace or ')'", stream.GetLocation(stream.Offset + 1));

                stream.SkipWhileWhiteSpace();
            }
        }

        public static Node ParseQuotedAtom(TextSourceReader stream)
        {
            int start = stream.Offset;

            if (stream.Read() != '"')
                throw new ParseException("Quoted atom must start with '\"'", stream.GetLocation(start));

            var inner = "";

            while (true)
            {
                switch (stream.Peek())
                {
                    case '\\':
                        {
                            stream.Read();

                            if (stream.ReadIf('\"')) { inner += "\""; }
                            else if (stream.ReadIf('\\')) { inner += "\\"; }
                            else if (stream.ReadIf('n')) { inner += "\n"; }
                            else if (stream.ReadIf('r')) { inner += "\r"; }
                            else if (stream.ReadIf('t')) { inner += "\t"; }
                            else if (stream.ReadIf('b')) { inner += "\b"; }
                            else if (stream.ReadIf('f')) { inner += "\f"; }
                            else if (stream.ReadIf('v')) { inner += "\v"; }
                            else if (stream.ReadIf('0')) { inner += "\0"; }
                            else if (stream.ReadIf('u'))
                            {
                                if (!stream.ReadWhile(Utility.IsHex, out var result, maxChars: 4))
                                    throw new ParseException("Invalid unicode escape sequence", stream.GetLocation(stream.Offset + 1));

                                inner += (char)int.Parse(result, NumberStyles.HexNumber);
                            }
                            else { throw new ParseException("Expected escapable character following '\\'", stream.GetLocation(stream.Offset + 1)); }
                        }
                        break;
                    case '"':
                        {
                            stream.Read();
                            return new Node(new Atom.Quoted(inner), stream.GetLocation(start));
                        }
                    case '\0': throw new ParseException("Expected '\"' but found EOF", stream.GetLocation(stream.Offset));
                    default: inner += stream.Read(); break;
                }
            }
        }

        public static Node ParseUnquotedAtom(TextSourceReader stream)
        {
            int start = stream.Offset;
            var value = stream.ReadWhile(c => Atom.Unquoted.Regex.IsMatch(c.ToString()));

            if (value.Length == 0)
                throw new ParseException("Encountered invalid character to start unquoted atom", stream.GetLocation(stream.Offset + 1));

            return new Node(new Atom.Unquoted(value), stream.GetLocation(start));
        }

        public static Node Parse(TextSourceReader stream)
        {
            stream.SkipWhileWhiteSpace();

            return stream.Peek() switch
            {
                '(' => ParseList(stream),
                '"' => ParseQuotedAtom(stream),
                _ => ParseUnquotedAtom(stream)
            };
        }
    }
}