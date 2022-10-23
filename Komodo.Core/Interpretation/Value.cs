using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public abstract record Value
{
    public UInt64 ByteSize => DataType.ByteSize;

    public abstract DataType DataType { get; }
    protected abstract SExpression ValueAsSExpression { get; }

    public abstract Byte[] AsBytes();

    public SExpression AsSExpression() => new SExpression.List(new[] {
        DataType.AsSExpression(),
        ValueAsSExpression
    });

    public sealed override string ToString() => AsSExpression().ToString();

    public virtual Value ConvertTo(DataType dataType) => throw new Exception($"Invalid conversion from {DataType} to {dataType}");

    public record I8(SByte Value) : Value
    {
        public override DataType DataType => new DataType.I8();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => new Byte[] { (Byte)Value };
    }

    public record UI8(Byte Value) : Value
    {
        public override DataType DataType => new DataType.UI8();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => new Byte[] { Value };
    }

    public record I16(Int16 Value) : Value
    {
        public override DataType DataType => new DataType.I16();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record UI16(UInt16 Value) : Value
    {
        public override DataType DataType => new DataType.UI16();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record I32(Int32 Value) : Value
    {
        public override DataType DataType => new DataType.I32();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record UI32(UInt32 Value) : Value
    {
        public override DataType DataType => new DataType.UI32();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record I64(Int64 Value) : Value
    {
        public override DataType DataType => new DataType.I64();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record UI64(UInt64 Value) : Value
    {
        public override DataType DataType => new DataType.UI64();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record F32(Single Value) : Value
    {
        public override DataType DataType => new DataType.F32();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record F64(Double Value) : Value
    {
        public override DataType DataType => new DataType.F64();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(Value.ToString());

        public override Byte[] AsBytes() => BitConverter.GetBytes(Value);
    }

    public record Bool : Value
    {
        public Byte Value { get; }

        public override DataType DataType => new DataType.Bool();
        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(IsTrue ? "true" : "false");

        public static Bool True => new Bool(1);
        public static Bool False => new Bool(0);

        public bool IsTrue => this == True;
        public bool IsFalse => this == False;

        public Bool(byte value) { Value = value == 0 || value == 1 ? value : throw new Exception($"Internal value for Bool can only be 0 or 1 but found {value}"); }
        public Bool(bool value) : this(value ? True.Value : False.Value) { }

        public void Deconstruct(out byte value) => value = Value;

        public override Byte[] AsBytes() => new Byte[] { Value };

        public static implicit operator bool(Bool b) => b.IsTrue;
    }

    public record Pointer(Address Address, DataType.Pointer PointerType) : Value
    {
        public override DataType DataType => PointerType;

        public bool IsReadonly => PointerType.IsReadonly;
        public bool IsReadWrite => PointerType.IsReadWrite;
        public bool IsNull => Address.IsNull;

        protected override SExpression ValueAsSExpression => Address.AsSExpression();

        public override Byte[] AsBytes() => Address.AsBytes();
    }

    public record Array(DataType ElementType, Address Address) : Value
    {
        public override DataType DataType => new DataType.Array(ElementType);
        protected override SExpression ValueAsSExpression => Address.AsSExpression();

        public bool Allocated => !Address.IsNull;
        public Address LengthStart => Address;
        public Address ElementsStart => Address + DataType.ByteSizeOf<DataType.UI64>();

        public override Byte[] AsBytes() => Address.AsBytes();
    }

    public record Reference(DataType ValueType, Address Address) : Value
    {
        public override DataType DataType => new DataType.Reference(ValueType);
        protected override SExpression ValueAsSExpression => Address.AsSExpression();

        public bool Allocated => !Address.IsNull;

        public override Byte[] AsBytes() => Address.AsBytes();
    }

    public record Function(VSROCollection<DataType> Parameters, VSROCollection<DataType> Returns, Address Address) : Value
    {
        public override DataType DataType => new DataType.Function(Parameters, Returns);
        protected override SExpression ValueAsSExpression => Address.AsSExpression();

        public bool Allocated => !Address.IsNull;

        public override Byte[] AsBytes() => Address.AsBytes();
    }

    public T As<T>() where T : Value => this as T ?? throw new Exception($"Value is not a {typeof(T)}.");

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
            DataType.Array(var elementType) => new Array(elementType, Address.FromBytes(byteArray)),
            DataType.Reference(var valueType) => new Reference(valueType, Address.FromBytes(byteArray)),
            DataType.Function(var parameters, var returns) => new Function(parameters, returns, Address.FromBytes(byteArray)),
            DataType.Pointer type => new Pointer(Address.FromBytes(byteArray), type),
            _ => throw new NotImplementedException(dataType.ToString())
        };
    }
}