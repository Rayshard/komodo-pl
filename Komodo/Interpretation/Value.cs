using Komodo.Compilation.Bytecode;

namespace Komodo.Interpretation;

public interface Value
{
    public record I64(Int64 Value) : Value;

    public DataType DataType => this switch
    {
        I64 => DataType.I64,
        _ => throw new NotImplementedException(this.ToString())
    };
}