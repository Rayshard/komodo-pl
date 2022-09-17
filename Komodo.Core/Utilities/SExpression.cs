using System.Globalization;
using System.Text.RegularExpressions;

namespace Komodo.Core.Utilities;

public abstract record SExpression(TextLocation? Location)
{
    public record UnquotedSymbol : SExpression
    {
        public static readonly Regex Regex = new Regex("^[^\\s\"\\(\\),]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public string Value { get; }

        public UnquotedSymbol(string value, TextLocation? location = null) : base(location) => Value = VerifyValue(value);

        public UnquotedSymbol ExpectValue(string value)
            => Value == value ? this : throw new FormatException($"Expected {value}, but found {Value}", this);

        public UnquotedSymbol ExpectValue(params string[] options)
            => options.Contains(Value) ? this : throw new FormatException($"Expected one of {Utility.Stringify(options, ", ", ("(", ")"))}, but found {Value}", this);

        public UnquotedSymbol ExpectValue(Regex pattern)
            => pattern.IsMatch(Value) ? this : throw new FormatException($"Expected value to match patttern \"{pattern}\", but found {Value}", this);

        public override string ToString() => Value;

        private static string VerifyValue(string value)
            => Regex.IsMatch(value) ? value : throw new InvalidOperationException($"Invalid unquoted symbol value: {value}");

        public override bool Matches(SExpression other) => other switch
        {
            UnquotedSymbol u => u.Value == Value,
            _ => false
        };

        new public static UnquotedSymbol Parse(TextSourceReader stream)
        {
            int start = stream.Offset;
            var value = stream.ReadWhile(c => UnquotedSymbol.Regex.IsMatch(c.ToString()));

            if (value.Length == 0)
                throw new ParseException("Encountered invalid character to start unquoted atom", stream.GetLocation(stream.Offset + 1));

            return new UnquotedSymbol(value, stream.GetLocation(start));
        }
    }

    public record QuotedSymbol(string Value, TextLocation? Location = null) : SExpression(Location)
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

        public override bool Matches(SExpression other) => other switch
        {
            QuotedSymbol q => q.Value == Value,
            _ => false
        };

        new public static QuotedSymbol Parse(TextSourceReader stream)
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
                            return new QuotedSymbol(inner, stream.GetLocation(start));
                        }
                    case '\0': throw new ParseException("Expected '\"' but found EOF", stream.GetLocation(stream.Offset));
                    default: inner += stream.Read(); break;
                }
            }
        }
    }

    public record List(IEnumerable<SExpression> Items, TextLocation? Location = null) : SExpression(Location), IEnumerable<SExpression>
    {
        public List(TextLocation? location = null) : this(new SExpression[] { }, location) { }

        public List ExpectLength(int length)
            => Items.Count() == length ? this : throw new FormatException($"Expected list of length {length}, but found list of length {Items.Count()}", this);

        public List ExpectLength(uint? min, uint? max)
        {
            if (min.HasValue && max.HasValue && max < min)
                throw new ArgumentException($"'min={min}' cannot be greater than 'max={max}'");
            else if (min.HasValue && Items.Count() < min)
                throw new FormatException($"Expected list of length at least {min}, but found list of length {Items.Count()}", this);
            else if (max.HasValue && Items.Count() > max)
                throw new FormatException($"Expected list of length at most {max}, but found list of length {Items.Count()}", this);
            else
                return this;
        }

        public List ExpectItem(int index, Action<SExpression> validator)
        {
            SExpression item;

            try { item = Items.ElementAt(index); }
            catch (ArgumentOutOfRangeException) { throw new ArgumentException($"Index {index} is outside of list range [0, {Items.Count() - 1}]."); }

            validator(item);
            return this;
        }

        public List ExpectItems(Action<SExpression, int> validator)
        {
            foreach (var (item, i) in Items.Select((item, i) => (item, i)))
                validator(item, i);

            return this;
        }

        public List ExpectItems(Action<SExpression> validator) => ExpectItems((item, i) => validator(item));

        public override string ToString() => Utility.Stringify(Items, " ", ("(", ")"));

        public override bool Matches(SExpression other) => other switch
        {
            List l => l.Items.Count() == Items.Count() && l.Items.Zip(Items).All(pair => pair.First.Matches(pair.Second)), 
            _ => false
        };

        public IEnumerator<SExpression> GetEnumerator() => Items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public SExpression this[int idx] => Items.ElementAt(idx);

        new public static List Parse(TextSourceReader stream)
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
                            return new List(items, stream.GetLocation(start));
                        }
                    case '\0': throw new ParseException("Expected ')' but found EOF", stream.GetLocation(stream.Offset));
                    default: items.Add(SExpression.Parse(stream)); break;
                }

                if (!stream.PeekIf(c => Char.IsWhiteSpace(c) || c == ')').HasValue)
                    throw new ParseException("Expected whitespace or ')'", stream.GetLocation(stream.Offset + 1));

                stream.SkipWhileWhiteSpace();
            }
        }
    }

    public class ParseException : Exception
    {
        public TextLocation Location { get; }

        public ParseException(string message, TextLocation location) : base(message) => Location = location;
    }

    public static SExpression Parse(TextSourceReader stream)
    {
        stream.SkipWhileWhiteSpace();

        return stream.Peek() switch
        {
            '(' => List.Parse(stream),
            '"' => QuotedSymbol.Parse(stream),
            _ => UnquotedSymbol.Parse(stream)
        };
    }

    #region Formatting
    public class FormatException : Exception
    {
        public SExpression Node { get; }

        public TextLocation? Location => Node.Location;

        public FormatException(string message, SExpression node) : base(message) => Node = node;
    }

    public abstract bool Matches(SExpression other);

    public UnquotedSymbol ExpectUnquotedSymbol()
        => this as UnquotedSymbol ?? throw new FormatException($"Expected unquoted symbol, but found {this.GetType()}", this);

    public QuotedSymbol ExpectQuotedSymbol()
        => this as QuotedSymbol ?? throw new FormatException($"Expected quoted symbol, but found {this.GetType()}", this);

    public List ExpectList()
        => this as List ?? throw new FormatException($"Expected list, but found {this.GetType()}", this);

    public bool IsList() => this is List;
    public bool IsQuotedSymbol() => this is QuotedSymbol;
    public bool IsUnquotedSymbol() => this is UnquotedSymbol;

    public void ExpectEnum<T>(T value) where T : struct, Enum
        => ExpectUnquotedSymbol().ExpectValue(value.ToString());

    public T AsEnum<T>() where T : struct, Enum
    {
        var value = ExpectUnquotedSymbol().Value;

        if (Enum.TryParse<T>(value, false, out var result))
            return result;

        throw new FormatException($"'{value}' is not a valid {typeof(T)}", this);
    }

    public Int64 AsInt64()
    {
        var value = ExpectUnquotedSymbol().Value;

        if (Int64.TryParse(value, out var result))
            return result;

        throw new FormatException($"'{value}' is not an Int64", this);
    }

    public UInt64 AsUInt64()
    {
        var value = ExpectUnquotedSymbol().Value;

        if (UInt64.TryParse(value, out var result))
            return result;

        throw new FormatException($"'{value}' is not a UInt64", this);
    }

    public bool AsBool()
    {
        var value = ExpectUnquotedSymbol().Value;

        if (bool.TryParse(value, out var result))
            return result;

        throw new FormatException($"'{value}' is not a bool", this);
    }
    #endregion
}