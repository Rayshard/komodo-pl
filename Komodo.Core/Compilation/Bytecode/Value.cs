using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record Value(DataType DataType)
{
    protected abstract SExpression ValueAsSExpression { get; }

    public SExpression AsSExpression() => new SExpression.List(new[] {
        DataType.AsSExpression(),
        ValueAsSExpression
    });

    public record I64(Int64 Value) : Value(new DataType.I64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override string ToString() => $"I64({Value})";

        new public static I64 Deserialize(SExpression sexpr)
        {
            if (sexpr is SExpression.List list)
            {
                list.ExpectLength(2);
                list[0].Expect(DataType.I64.Deserialize);
                return new I64(list[1].ExpectInt64());
            }
            else { return new I64(sexpr.ExpectInt64()); }
        }
    }

    public record Bool(bool Value) : Value(new DataType.Bool())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value ? "true" : "false");

        public override string ToString() => Value ? "Bool(true)" : "Bool(false)";

        new public static Bool Deserialize(SExpression sexpr)
        {
            if (sexpr is SExpression.List list)
            {
                list.ExpectLength(2);
                list[0].Expect(DataType.Bool.Deserialize);
                return new Bool(list[1].ExpectBool());
            }
            else { return new Bool(sexpr.ExpectBool()); }
        }
    }

    public record Array(DataType ElementType, List<Value> Elements) : Value(new DataType.Array(ElementType))
    {
        protected override SExpression ValueAsSExpression => new SExpression.List(Elements.Select(element => element.AsSExpression()));

        public override string ToString() => Utility.Stringify(Elements, ", ", ("[", "]"));

        new public static Array Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, null);

            if (list.Count() == 2)
            {
                try
                {
                    var elementType = list.Expect(DataType.Array.Deserialize).ElementType;
                    return new Array(elementType, new List<Value>());
                }
                catch
                {
                    var elementType = list[0].Expect(DataType.Array.Deserialize).ElementType;
                    var elements = list[1].ExpectList().Select(Value.Deserialize).ToList();

                    return new Array(elementType, elements);
                }
            }
            else
            {
                list[0].ExpectUnquotedSymbol().ExpectValue("Array");

                var elementType = list[1].Expect(DataType.Deserialize);
                var elements = list.Skip(2).Select(Value.Deserialize).ToList();

                return new Array(elementType, elements);
            }
        }
    }

    public T As<T>() where T : Value => this as T ?? throw new Exception($"Value is not a {typeof(T)}.");

    public static Value CreateDefault(DataType dataType) => dataType switch
    {
        DataType.I64 => new I64(0),
        DataType.Bool => new Bool(false),
        DataType.Array(var elementType) => new Array(elementType, new List<Value>()),
        _ => throw new NotImplementedException(dataType.ToString())
    };

    public static Value Deserialize(SExpression sexpr)
    {
        try { return I64.Deserialize(sexpr); }
        catch { }

        try { return Bool.Deserialize(sexpr); }
        catch { }

        try { return Array.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid value: {sexpr}", sexpr);
    }
}