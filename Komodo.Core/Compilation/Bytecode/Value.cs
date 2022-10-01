using System.Collections.ObjectModel;
using System.Text;
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

    public record UI64(UInt64 Value) : Value(new DataType.UI64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override string ToString() => $"UI64({Value})";

        new public static UI64 Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].Expect(DataType.UI64.Deserialize);

            return new UI64(list[1].ExpectUInt64());
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

    public record Char(UTF8Char Value) : Value(new DataType.Char())
    {
        protected override SExpression ValueAsSExpression
            => new SExpression.UnquotedSymbol($"'{Value.Representation}'");

        public override string ToString() => $"Char({Value.Representation})";

        new public static Char Deserialize(SExpression sexpr)
        {
            if (sexpr is SExpression.List list)
            {
                list.ExpectLength(2)
                    .ExpectItem(0, DataType.Char.Deserialize)
                    .ExpectItem(1, item => item.ExpectChar(), out var value);

                return new Char(value);
            }
            else { return new Char(sexpr.ExpectChar()); }
        }
    }

    public record Array(DataType ElementType) : Value(new DataType.Array(ElementType))
    {
        private List<Value> elements = new List<Value>();
        public ReadOnlyCollection<Value> Elements => elements.AsReadOnly();

        public Array(DataType elementType, IEnumerable<Value> elements) : this(elementType)
        {
            foreach (var element in elements)
            {
                if (element.DataType != ElementType)
                    throw new Exception($"Cannot add element of type '{element.DataType}' to an array of type '{DataType}'");

                this.elements.Add(element);
            }
        }

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

    public Value Expect(DataType dataType) => dataType switch
    {
        DataType.I64 when this is I64 => this,
        DataType.Bool when this is Bool => this,
        DataType.Array(var elementType) when this is Array a && a.DataType == elementType => this,
        _ => throw new Exception($"Invalid value cast: Expected {dataType}, but found {DataType}")
    };

    public static Value CreateDefault(DataType dataType) => dataType switch
    {
        DataType.I64 => new I64(0),
        DataType.Bool => new Bool(false),
        DataType.Char => new Char('\0'),
        DataType.Array(var elementType) => new Array(elementType, new List<Value>()),
        _ => throw new NotImplementedException(dataType.ToString())
    };

    public static Value Deserialize(SExpression sexpr)
    {
        try { return I64.Deserialize(sexpr); }
        catch { }

        try { return UI64.Deserialize(sexpr); }
        catch { }

        try { return Bool.Deserialize(sexpr); }
        catch { }

        try { return Array.Deserialize(sexpr); }
        catch { }

        try { return Char.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid value: {sexpr}", sexpr);
    }
}