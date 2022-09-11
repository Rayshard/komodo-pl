namespace Komodo.Compilation.Bytecode;

public enum Opcode
{
    PushI64,
    Syscall,
    Call,
    Return,
    LoadArg,
    JNZ,

    Add,
    Eq,
    Dec,
    Mul,
    Print,
}

public enum SyscallCode
{
    Exit,
}

public abstract record Instruction(Opcode Opcode)
{
    public record Syscall(SyscallCode Code) : Instruction(Opcode.Syscall);
    public record PushI64(Int64 Value) : Instruction(Opcode.PushI64);
    public record Add() : Instruction(Opcode.Add);
    public record Print() : Instruction(Opcode.Print);
    public record Call(string Module, string Function) : Instruction(Opcode.Call);
    public record LoadArg(UInt64 Index) : Instruction(Opcode.LoadArg);
    public record Eq() : Instruction(Opcode.Eq);
    public record Dec() : Instruction(Opcode.Dec);
    public record Mul() : Instruction(Opcode.Mul);
    public record JNZ(string BasicBlock) : Instruction(Opcode.JNZ);
    public record Return() : Instruction(Opcode.Return);
}