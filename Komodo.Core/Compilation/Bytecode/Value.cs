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

    public Value ReinterpretAs(DataType dataType) => DataType.ByteSize != dataType.ByteSize
        ? throw new Exception($"Cannot interpret {DataType} which has size {DataType.ByteSize} as {dataType} which has size {dataType.ByteSize}")
        : Create(dataType, AsBytes());

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

    public record I16(Int16 Value) : Value(new DataType.I16())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record UI16(UInt16 Value) : Value(new DataType.UI16())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record I32(Int32 Value) : Value(new DataType.I32())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record UI32(UInt32 Value) : Value(new DataType.UI32())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record I64(Int64 Value) : Value(new DataType.I64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);

        public override Value ConvertTo(DataType dataType) => dataType switch
        {
            DataType.I8 => new I8((SByte)Value),
            DataType.UI8 => new UI8((Byte)Value),
            DataType.I16 => new I16((Int16)Value),
            DataType.UI16 => new UI16((UInt16)Value),
            DataType.I32 => new I32((Int32)Value),
            DataType.UI32 => new UI32((UInt32)Value),
            DataType.I64 => new I64((Int64)Value),
            DataType.UI64 => new UI64((UInt64)Value),
            DataType.F32 => new F32((float)Value),
            DataType.F64 => new F64((double)Value),
            DataType.Bool => new Bool(Value != 0),
            _ => base.ConvertTo(dataType)
        };
    }

    public record UI64(UInt64 Value) : Value(new DataType.UI64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record F32(float Value) : Value(new DataType.F32())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record F64(double Value) : Value(new DataType.F64())
    {
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record Bool(Byte Value) : Value(new DataType.Bool())
    {
        public bool IsTrue => Value != 0;
        public bool IsFalse => Value == 0;

        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(IsTrue ? "false" : "true");

        public Bool(bool value) : this((Byte)(value ? 1 : 0)) { }

        public override Byte[] AsBytes() => new Byte[] { Value };
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

    public static Value Create(DataType dataType, IEnumerable<Byte> bytes)
    {
        var byteArray = bytes.ToArray();

        if ((UInt64)byteArray.Length != dataType.ByteSize)
            throw new Exception($"Cannot create {dataType} which has size {dataType.ByteSize} from {byteArray.Length} bytes");

        return dataType switch
        {
            DataType.I8 => new I8((SByte)byteArray[0]),
            DataType.UI8 => new UI8((Byte)byteArray[0]),
            DataType.I16 => new I16(BitConverter.ToInt16(byteArray)),
            DataType.UI16 => new UI16(BitConverter.ToUInt16(byteArray)),
            DataType.I32 => new I32(BitConverter.ToInt32(byteArray)),
            DataType.UI32 => new UI32(BitConverter.ToUInt32(byteArray)),
            DataType.I64 => new I64(BitConverter.ToInt64(byteArray)),
            DataType.UI64 => new UI64(BitConverter.ToUInt64(byteArray)),
            DataType.F32 => new F32(BitConverter.ToSingle(byteArray)),
            DataType.F64 => new F64(BitConverter.ToDouble(byteArray)),
            DataType.Bool => new Bool((Byte)byteArray[0]),
            DataType.Array(var elementType) => new Array(elementType, 0, Address.FromBytes(byteArray)),
            DataType.Type => new Type(Address.FromBytes(byteArray)),
            DataType.Reference(var valueType) => new Reference(valueType, Address.FromBytes(byteArray)),
            _ => throw new NotImplementedException(dataType.ToString())
        };
    }
}