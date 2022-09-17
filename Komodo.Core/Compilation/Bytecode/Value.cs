using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public enum DataType { I64, Bool, Array }

public abstract record Value(DataType DataType)
{
    private static Func<SExpression, Value>[] Deserializers = new Func<SExpression, Value>[]
    {
        sexpr => new Bool(sexpr.AsBool()),
        sexpr => new I64(sexpr.AsInt64()),
    };

    protected abstract SExpression ValueAsSExpression { get; }

    public record I64(Int64 Value) : Value(DataType.I64)
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override string ToString() => $"{DataType}({Value})";

        new public static I64 Deserialize(SExpression sexpr)
        {
            if (sexpr is SExpression.List list)
            {
                list.ExpectLength(2);
                list[0].ExpectEnum<DataType>(DataType.Bool);
                return new I64(list[1].AsInt64());
            }
            else { return new I64(sexpr.AsInt64()); }
        }
    }

    public record Bool(bool Value) : Value(DataType.Bool)
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());
        public override string ToString() => DataType.ToString() + "(" + (Value ? "true" : "false") + ")";

        new public static Bool Deserialize(SExpression sexpr)
        {
            if (sexpr is SExpression.List list)
            {
                list.ExpectLength(2);
                list[0].ExpectEnum<DataType>(DataType.Bool);
                return new Bool(list[1].AsBool());
            }
            else { return new Bool(sexpr.AsBool()); }
        }
    }

    public record Array(DataType ElementType, List<Value> Elements) : Value(DataType.Array)
    {
        protected override SExpression ValueAsSExpression => new SExpression.List(Elements.Select(element => element.AsSExpression()));
        public override string ToString() => DataType.ToString() + Utility.Stringify(Elements, ", ", ("(", ")"));

        new public static Array Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, null);
            list[0].ExpectEnum(DataType.Array);

            var elementType = list[1].AsEnum<DataType>();
            var elements = list.Skip(2).Select(Value.Deserialize).ToList();

            return new Array(elementType, elements);
        }
    }

    public T As<T>() where T : Value => this as T ?? throw new Exception("Value is not an I64.");

    public SExpression AsSExpression() => new SExpression.List(new[] {
        new SExpression.UnquotedSymbol(DataType.ToString()),
        ValueAsSExpression
    });

    public static Value CreateDefault(DataType dataType) => dataType switch
    {
        DataType.I64 => new I64(0),
        DataType.Bool => new Bool(false),
        _ => throw new NotImplementedException(dataType.ToString())
    };

    public static DataType GetDataType<T>() where T : Value => typeof(T) switch
    {
        Type t when t == typeof(I64) => DataType.I64,
        Type t when t == typeof(Bool) => DataType.Bool,
        var t => throw new NotImplementedException(t.ToString())
    };

    public static Value Deserialize(SExpression sexpr)
    {
        if (sexpr is SExpression.List list)
        {
            return list[0].AsEnum<DataType>() switch
            {
                DataType.I64 => I64.Deserialize(list[1]),
                DataType.Bool => Bool.Deserialize(list[1]),
                DataType.Array => Array.Deserialize(list[1]),
                var dt => throw new NotImplementedException(dt.ToString())
            };
        }
        else
        {
            foreach (var deserializer in Deserializers)
            {
                try { return deserializer(sexpr); }
                catch { continue; }
            }

            throw new SExpression.FormatException("Invalid value", sexpr);
        }
    }
}