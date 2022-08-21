namespace Komodo.Interpretation;

public record InstructionPointer(string Module, string Function, string BasicBlock, int Index)
{
    public override string ToString() => $"{Module}.{Function}.{BasicBlock}.{Index}";
}
