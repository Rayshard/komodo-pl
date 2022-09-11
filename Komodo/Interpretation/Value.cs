using Komodo.Compilation.Bytecode;

namespace Komodo.Interpretation;

public abstract record Value(DataType DataType)
{
    public record I64(Int64 Value) : Value(DataType.I64);
    public record Bool(bool Value) : Value(DataType.Bool);
}