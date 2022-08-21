namespace Komodo.Interpretation;

public record InstructionPointer(string Module, string Function, string BasicBlock, int Index)
{
    public override string ToString() => $"{Module}.{Function}.{BasicBlock}.{Index}";

    public static InstructionPointer operator +(InstructionPointer ip, int i)
        => new InstructionPointer(ip.Module, ip.Function, ip.BasicBlock, ip.Index + i);

    public static InstructionPointer operator -(InstructionPointer ip, int i)
        => new InstructionPointer(ip.Module, ip.Function, ip.BasicBlock, ip.Index - i);
}
