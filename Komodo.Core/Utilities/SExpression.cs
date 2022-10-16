using System.Globalization;
using System.Text.RegularExpressions;

namespace Komodo.Core.Utilities;

public abstract record SExpression(TextLocation? Location)
{
    public abstract bool Matches(SExpression other);

    public record UnquotedSymbol : SExpression
    {
        public static readonly Regex Regex = new Regex("^[^\\s\"\\(\\),]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public string Value { get; }

        public UnquotedSymbol(string value, TextLocation? location = null) : base(location) => Value = VerifyValue(value);

        public UnquotedSymbol ExpectValue(string value)
            => Value == value ? this : throw new FormatException($"Expected {value}, but found {Value}", this);

        public UnquotedSymbol ExpectValue(params string[] options)
            => options.Contains(Value) ? this : throw new FormatException($"Expected one of {Utility.Stringify(options, ", ", ("(", ")"))}, but found {Value}", this);

        public UnquotedSymbol ExpectValue(Regex pattern, out Match match)
        {
            match = pattern.Match(Value);
            return match.Success ? this : throw new FormatException($"Expected value to match patttern \"{pattern}\", but found {Value}", this);
        }

        public UnquotedSymbol ExpectValue(Regex pattern) => ExpectValue(pattern, out _);

        public override string ToString() => Value;

        private static string VerifyValue(string value)
            => Regex.IsMatch(value) ? value : throw new InvalidOperationException($"Invalid unquoted symbol value: {value}");

        public override bool Matches(SExpression other) => other is UnquotedSymbol u && u.Value == Value;

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

        public override bool Matches(SExpression other) => other is QuotedSymbol q && q.Value == Value;

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

    public record List(VSROCollection<SExpression> Items, TextLocation? Location = null) : SExpression(Location), IEnumerable<SExpression>
    {
        public List(IEnumerable<SExpression> items, TextLocation? location = null) : this(items.ToVSROCollection(), location) { }
        public List(TextLocation? location = null) : this(new SExpression[0], location) { }

        public List ExpectLength(int length)
            => Items.Count == length ? this : throw new FormatException($"Expected list of length {length}, but found list of length {Items.Count}", this);

        public List ExpectLength(uint? min, uint? max, out uint length)
        {
            length = (uint)Items.Count;

            if (min.HasValue && max.HasValue && max < min) { throw new ArgumentException($"'min={min}' cannot be greater than 'max={max}'"); }
            else if (min.HasValue && length < min) { throw new FormatException($"Expected list of length at least {min}, but found list of length {length}", this); }
            else if (max.HasValue && length > max) { throw new FormatException($"Expected list of length at most {max}, but found list of length {length}", this); }
            else { return this; }
        }

        public List ExpectLength(uint? min, uint? max) => ExpectLength(min, max, out var _);

        public List ExpectItem(int index, Action<SExpression> validator)
        {
            SExpression item;

            try { item = Items.ElementAt(index); }
            catch (ArgumentOutOfRangeException) { throw new ArgumentException($"Index {index} is outside of list range [0, {Items.Count - 1}]."); }

            validator(item);
            return this;
        }

        public List ExpectItem<T>(int index, Func<SExpression, T> validator, out T result)
        {
            SExpression item;

            try { item = Items.ElementAt(index); }
            catch (ArgumentOutOfRangeException) { throw new ArgumentException($"Index {index} is outside of list range [0, {Items.Count - 1}]."); }

            result = validator(item);
            return this;
        }

        public List ExpectItem(int index, SExpression template) => ExpectItem(index, item => item.Expect(template), out _);

        public List ExpectItem<T>(int index, Func<SExpression, T> validator) => ExpectItem(index, validator, out var _);

        public List ExpectItems<T>(Func<SExpression[], T> validator, out T result, int start = 0)
        {
            result = validator(Items.Skip(start).ToArray());
            return this;
        }

        public List ExpectItems(Action<SExpression[]> validator, int start = 0)
        {
            validator(Items.Skip(start).ToArray());
            return this;
        }

        public List ExpectItems<T>(Func<SExpression, T> validator, out T[] result, int start = 0)
            => ExpectItems(items => items.Select(validator).ToArray(), out result, start);

        public List ExpectItems(Action<SExpression> validator, int start = 0)
            => ExpectItems(items => items.ForEach(validator), start);

        public override string ToString() => Utility.Stringify(Items, " ", ("(", ")"));

        public override bool Matches(SExpression other) => other is List l && l.Items == Items;

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

    public class FormatException : Exception
    {
        public SExpression Node { get; }

        public TextLocation? Location => Node.Location;

        public FormatException(string message, SExpression node) : base(message) => Node = node;

        public static FormatException Expected(object expected, object actual, SExpression node) => new FormatException($"Expected {expected}, but found {actual}", node);
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

    public static UnquotedSymbol UInt64(UInt64 value) => new UnquotedSymbol(value.ToString());
}

public static class SExpressionExtensions
{
    public static T Expect<T>(this SExpression sexpr, Func<SExpression, T> deserializer) => deserializer(sexpr);

    public static SExpression Expect(this SExpression sexpr, SExpression template) => sexpr.Matches(template) ? sexpr : throw SExpression.FormatException.Expected(template, sexpr, sexpr);

    public static SExpression.UnquotedSymbol ExpectUnquotedSymbol(this SExpression sexpr)
        => sexpr as SExpression.UnquotedSymbol ?? throw SExpression.FormatException.Expected("unquoted symbol", sexpr.GetType(), sexpr);

    public static SExpression.QuotedSymbol ExpectQuotedSymbol(this SExpression sexpr)
        => sexpr as SExpression.QuotedSymbol ?? throw SExpression.FormatException.Expected($"quoted symbol", sexpr.GetType(), sexpr);

    public static SExpression.List ExpectList(this SExpression sexpr)
        => sexpr as SExpression.List ?? throw SExpression.FormatException.Expected("list", sexpr.GetType(), sexpr);

    public static T ExpectEnum<T>(this SExpression sexpr, T? expected = null) where T : struct, Enum
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (Enum.TryParse<T>(value, false, out var result))
        {
            if (expected is not null && !result.Equals(expected))
                throw SExpression.FormatException.Expected(expected, result, sexpr);

            return result;
        }

        if (expected is not null) { throw SExpression.FormatException.Expected(expected, result, sexpr); }
        else { throw SExpression.FormatException.Expected($"one of {Utility.Stringify<T>(", ")}", value, sexpr); }
    }

    public static SByte ExpectInt8(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (SByte.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 8-bit signed integer", sexpr);
    }

    public static Byte ExpectUInt8(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (Byte.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 8-bit unsigned integer", sexpr);
    }

    public static Int16 ExpectInt16(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (Int16.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not an 16-bit signed integer", sexpr);
    }

    public static UInt16 ExpectUInt16(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (System.UInt16.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 16-bit unsigned integer", sexpr);
    }

    public static Int32 ExpectInt32(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (Int32.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not an 32-bit signed integer", sexpr);
    }

    public static UInt32 ExpectUInt32(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (System.UInt32.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 32-bit unsigned integer", sexpr);
    }

    public static Int64 ExpectInt64(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (Int64.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not an 64-bit signed integer", sexpr);
    }

    public static UInt64 ExpectUInt64(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (System.UInt64.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 64-bit unsigned integer", sexpr);
    }

    public static Single ExpectFloat(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (System.Single.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 32-bit floating-point", sexpr);
    }

    public static Double ExpectDouble(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (System.Double.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a 64-bit floating-point", sexpr);
    }

    public static bool ExpectBool(this SExpression sexpr)
    {
        var value = sexpr.ExpectUnquotedSymbol().Value;

        if (bool.TryParse(value, out var result))
            return result;

        throw new SExpression.FormatException($"'{value}' is not a bool", sexpr);
    }

    public static bool IsList(this SExpression sexpr) => sexpr is SExpression.List;
    public static bool IsQuotedSymbol(this SExpression sexpr) => sexpr is SExpression.QuotedSymbol;
    public static bool IsUnquotedSymbol(this SExpression sexpr) => sexpr is SExpression.UnquotedSymbol;

    public static bool IsInt8(this SExpression sexpr)
    {
        try
        {
            sexpr.ExpectInt8();
            return true;
        }
        catch { return false; }
    }

    public static bool IsUInt8(this SExpression sexpr)
    {
        try
        {
            sexpr.ExpectUInt8();
            return true;
        }
        catch { return false; }
    }

    public static bool IsInt64(this SExpression sexpr)
    {
        try
        {
            sexpr.ExpectInt64();
            return true;
        }
        catch { return false; }
    }

    public static bool IsUInt64(this SExpression sexpr)
    {
        try
        {
            sexpr.ExpectUInt64();
            return true;
        }
        catch { return false; }
    }

    public static bool IsBool(this SExpression sexpr)
    {
        try
        {
            sexpr.ExpectBool();
            return true;
        }
        catch { return false; }
    }

    public static bool IsEnum<T>(this SExpression sexpr, T value) where T : struct, Enum
    {
        try
        {
            sexpr.ExpectEnum<T>();
            return true;
        }
        catch { return false; }
    }
}