using Komodo.Utilities;
using System.Text.RegularExpressions;

namespace Komodo.Compilation.Bytecode;

public enum DataType { I8, I16, I32, I64, F32, F64, Bool }

public abstract record Value(DataType DataType)
{
    private static (Regex Regex, Func<string, Value> Deserializer)[] SymbolDeserializers = new (Regex, Func<string, Value>)[]
    {
        (new Regex("^(true|false)$"), value => new Bool(bool.Parse(value))),
        (new Regex("^(0|(-?[1-9][0-9]*))$"), value => new I64(Int64.Parse(value))),
    };

    protected abstract SExpression ValueAsSExpression { get; }

    public record I64(Int64 Value) : Value(DataType.I64)
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override string ToString() => $"{DataType}({Value})";

        new public static I64 Deserialize(SExpression sexpr) => new I64(sexpr.AsInt64());

    }

    public record Bool(bool Value) : Value(DataType.Bool)
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());
        public override string ToString() => DataType.ToString() + "(" + (Value ? "true" : "false") + ")";

        new public static Bool Deserialize(SExpression sexpr) => new Bool(sexpr.AsBool());
    }

    public SExpression AsSExpression() => new SExpression.List(new[] {
        new SExpression.UnquotedSymbol(DataType.ToString()),
        ValueAsSExpression
    });

    public static Value Deserialize(SExpression sexpr)
    {
        if (sexpr.IsList())
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            return list[0].AsEnum<DataType>() switch
            {
                DataType.I64 => I64.Deserialize(list[1]),
                DataType.Bool => Bool.Deserialize(list[1]),
                var dt => throw new NotImplementedException(dt.ToString())
            };
        }
        else
        {
            var value = sexpr.ExpectUnquotedSymbol().ExpectValue(Utility.Concat(SymbolDeserializers.Select(sd => sd.Regex))).Value;

            foreach (var (regex, deserializer) in SymbolDeserializers)
            {
                if (regex.IsMatch(value))
                    return deserializer(value);
            }

            throw new SExpression.FormatException("Invalid value", sexpr);
        }
    }
}