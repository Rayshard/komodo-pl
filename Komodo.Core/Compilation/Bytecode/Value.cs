using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record Value(DataType DataType)
{
    protected abstract SExpression ValueAsSExpression { get; }

    public abstract Byte[] AsBytes();

    public SExpression AsSExpression() => new SExpression.List(new[] {
        DataType.AsSExpression(),
        ValueAsSExpression
    });

    public sealed override string ToString() => AsSExpression().ToString();

    public record UI8(Byte Value) : Value(new DataType.UI8())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => new Byte[] { Value };
    }

    public record I64(Int64 Value) : Value(new DataType.I64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record UI64(UInt64 Value) : Value(new DataType.UI64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record Bool(bool Value) : Value(new DataType.Bool())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value ? "true" : "false");

        public override Byte[] AsBytes() => new Byte[] { Value ? (byte)1 : (byte)0 };
    }

    public record Array(DataType ElementType, UInt64 Length, UInt64 Address) : Value(new DataType.Array(ElementType))
    {
        public override Byte[] AsBytes() => new[] { BitConverter.GetBytes(Length), BitConverter.GetBytes(Address) }.Flatten().ToArray();

        protected override SExpression ValueAsSExpression => new SExpression.List(new[] {
            new SExpression.List(new[] { new SExpression.UnquotedSymbol("address"), new SExpression.UnquotedSymbol($"0x{Address.ToString("X")}")} ),
            new SExpression.List(new[] { new SExpression.UnquotedSymbol("length"), SExpression.UInt64(Length) })
        });
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
        DataType.UI8 => new UI8(0),
        DataType.I64 => new I64(0),
        DataType.UI64 => new UI64(0),
        DataType.Bool => new Bool(false),
        DataType.Array(var elementType) => new Array(elementType, 0, 0),
        _ => throw new NotImplementedException(dataType.ToString())
    };
}