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

    public record Bool(Byte Value) : Value
    {
        public override DataType DataType => new DataType.Bool();
        
        public static Bool True => new Bool(1);
        public static Bool False => new Bool(0);

        public bool IsTrue => this == True;
        public bool IsFalse => this == False;

        public Bool(bool value) : this(value ? True.Value : False.Value) { }

        protected override SExpression ValueAsSExpression => new SExpression.UnquotedSymbol(IsTrue ? "true" : "false");

        public override Byte[] AsBytes() => new Byte[] { Value };

        private static Byte VerifyValue(Byte value) => value == 0 || value == 1 ? value : throw new Exception($"Internal value for Bool can only be 0 or 1 but found {value}");
    }

    public abstract record Pointer(Address Address) : Value
    {
        public bool Allocated => !Address.IsNull;

        protected override SExpression ValueAsSExpression => Address.AsSExpression();

        public override Byte[] AsBytes() => Address.AsBytes();
    }

    public record Array(DataType ElementType, Address Address) : Pointer(Address)
    {
        public override DataType DataType => new DataType.Array(ElementType);
        
        public Address LengthStart => Address;
        public Address ElementsStart => Address + DataType.ByteSizeOf<DataType.UI64>();
    }

    public record Type(Address Address) : Pointer(Address)
    {
        public override DataType DataType => new DataType.Type();
    }

    public record Reference(DataType ValueType, Address Address) : Pointer(Address)
    {
        public override DataType DataType => new DataType.Reference(ValueType);
    }

    public record Function(VSROCollection<DataType> Parameters, VSROCollection<DataType> Returns, Address Address) : Pointer(Address)
    {
        public override DataType DataType => new DataType.Function(Parameters, Returns);
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
            DataType.Type => new Type(Address.FromBytes(byteArray)),
            DataType.Reference(var valueType) => new Reference(valueType, Address.FromBytes(byteArray)),
            DataType.Function(var parameters, var returns) => new Function(parameters, returns, Address.FromBytes(byteArray)),
            _ => throw new NotImplementedException(dataType.ToString())
        };
    }
}