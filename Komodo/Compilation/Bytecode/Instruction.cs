namespace Komodo.Compilation.Bytecode;

public enum Opcode
{
    PushI64,
    AddI64,
    EqI64,
    DecI64,
    MulI64,
    PrintI64,
    Syscall,
    Call,
    Return,
    LoadArg,
    JNZ,
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

    public record PrintI64() : Instruction(Opcode.PrintI64)
    {
        public override string ToString() => "PrintI64";
    }

    public record Call(string Module, string Function) : Instruction(Opcode.Call)
    {
        public override string ToString() => $"Call {Module} {Function}";
    }

    public record LoadArg(UInt64 Index) : Instruction(Opcode.LoadArg)
    {
        public override string ToString() => $"LoadArg {Index}";
    }

    public record EqI64() : Instruction(Opcode.EqI64)
    {
        public override string ToString() => "EqI64";
    }

    public record DecI64() : Instruction(Opcode.DecI64)
    {
        public override string ToString() => "DecI64";
    }

    public record MulI64() : Instruction(Opcode.MulI64)
    {
        public override string ToString() => "MulI64";
    }

    public record JNZ(string BasicBlock) : Instruction(Opcode.JNZ)
    {
        public override string ToString() => "JNZ";
    }

    public record Return() : Instruction(Opcode.Return)
    {
        public override string ToString() => "Return";
    }
}