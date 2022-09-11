using Komodo.Compilation.Bytecode;

namespace Komodo.Interpretation;

public abstract record Value(DataType DataType)
{
    public record I64(Int64 Value) : Value(DataType.I64)
    {
        public override string ToString() => $"{DataType}({DataType})";
    }

    public record Bool(bool Value) : Value(DataType.Bool)
    {
        public override string ToString() => DataType.ToString() + "(" + (Value ? "true" : "false")  + ")";
    }
}