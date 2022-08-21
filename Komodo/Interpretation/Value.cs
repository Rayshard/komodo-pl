namespace Komodo.Interpretation;

public interface Value
{
    public record I64(Int64 Value) : Value;
}