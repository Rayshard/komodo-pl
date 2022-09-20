namespace Komodo.Core.Interpretation;

public record InstructionPointer(string Module, string Function, UInt64 Index)
{
    public override string ToString() => $"{Module}.{Function}.{Index}";

    public static InstructionPointer operator +(InstructionPointer ip, UInt64 i)
        => new InstructionPointer(ip.Module, ip.Function, ip.Index + i);

}
