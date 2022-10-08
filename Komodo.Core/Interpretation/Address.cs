using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public record Address(UInt64 Value)
{
    public static readonly UInt64 ByteSize = 8;
    public static readonly Address NULL = new Address(0ul);

    public bool IsNull => this == NULL;

    public Byte[] AsBytes() => BitConverter.GetBytes(Value);

    public override string ToString() => $"0x{Value.ToString("X")}";

    public SExpression AsSExpression() => new SExpression.List(new[] {
        new SExpression.UnquotedSymbol("address"),
        new SExpression.UnquotedSymbol(ToString())
    });

    public static implicit operator UInt64(Address a) => a.Value;
    public static implicit operator Address(UInt64 value) => new Address(value);
    public static Address FromBytes(ReadOnlySpan<Byte> bytes) => new Address(BitConverter.ToUInt64(bytes));
}