namespace Komodo.Compilation.Bytecode;

public enum Opcode
{
    PushI64,
    AddI64,
    Syscall,
}

public enum SyscallCode
{
    Exit,
}

public abstract record Instruction(Opcode Opcode)
{
    public record Syscall(SyscallCode Code) : Instruction(Opcode.Syscall)
    {
        public override string ToString() => $"Syscall {Code}";
    }

    public record PushI64(Int64 Value) : Instruction(Opcode.PushI64)
    {
        public override string ToString() => $"PushI64 {Value}";
    }

    public record AddI64() : Instruction(Opcode.AddI64)
    {
        public override string ToString() => "AddI64";
    }
}