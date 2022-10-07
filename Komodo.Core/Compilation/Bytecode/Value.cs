using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record Value(DataType DataType)
{
    public UInt64 ByteSize => DataType.ByteSize;

    protected abstract SExpression ValueAsSExpression { get; }

    public abstract Byte[] AsBytes();

    public SExpression AsSExpression() => new SExpression.List(new[] {
        DataType.AsSExpression(),
        ValueAsSExpression
    });

    public sealed override string ToString() => AsSExpression().ToString();

    public virtual Value ConvertTo(DataType dataType) => throw new Exception($"Invalid conversion from {DataType} to {dataType}");

    public record I8(SByte Value) : Value(new DataType.I8())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => new Byte[] { (Byte)Value };
    }

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

    public record Array(DataType ElementType, UInt64 Length, Address Address) : Value(new DataType.Array(ElementType))
    {
        public override Byte[] AsBytes() => new[] { BitConverter.GetBytes(Length), Address.AsBytes() }.Flatten().ToArray();

        protected override SExpression ValueAsSExpression => new SExpression.List(new[] {
            Address.AsSExpression(),
            new SExpression.List(new[] { new SExpression.UnquotedSymbol("length"), SExpression.UInt64(Length) })
        });
    }

    public record Type(Address Address) : Value(new DataType.Type())
    {
        public bool IsUnknown => Address.IsNull;

        public override Byte[] AsBytes() => Address.AsBytes();

        protected override SExpression ValueAsSExpression => Address.AsSExpression();
    }

    public record Reference(DataType ValueType, Address Address) : Value(new DataType.Reference(ValueType))
    {
        public bool IsSet => !Address.IsNull;

        public override Byte[] AsBytes() => Address.AsBytes();

        protected override SExpression ValueAsSExpression => Address.AsSExpression();
    }

    public T As<T>() where T : Value => this as T ?? throw new Exception($"Value is not a {typeof(T)}.");

    public static Value CreateDefault(DataType dataType) => dataType switch
    {
        DataType.I8 => new I8(0),
        DataType.UI8 => new UI8(0),
        DataType.I64 => new I64(0),
        DataType.UI64 => new UI64(0),
        DataType.Bool => new Bool(false),
        DataType.Array(var elementType) => new Array(elementType, 0, Address.NULL),
        DataType.Type => new Type(Address.NULL),
        DataType.Reference(var valueType) => new Reference(valueType, Address.NULL),
        _ => throw new NotImplementedException(dataType.ToString())
    };
}