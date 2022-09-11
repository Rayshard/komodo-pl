using Komodo.Utilities;

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
    protected abstract IEnumerable<SExpression> OperandsAsSExpressions { get; }

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol(Opcode.ToString()));
        nodes.AddRange(OperandsAsSExpressions);
        return new SExpression.List(nodes);
    }

    public record Syscall(SyscallCode Code) : Instruction(Opcode.Syscall)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(Code.ToString()) };
    }

    public record PushI64(Int64 Value) : Instruction(Opcode.PushI64)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(Value.ToString()) };
    }

    public record Add() : Instruction(Opcode.Add)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }

    public record Print() : Instruction(Opcode.Print)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }

    public record Call(string Module, string Function) : Instruction(Opcode.Call)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[]
        {
            new SExpression.UnquotedSymbol(Module),
            new SExpression.UnquotedSymbol(Function),
        };
    }

    public record LoadArg(UInt64 Index) : Instruction(Opcode.LoadArg)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(Index.ToString()) };
    }

    public record Eq() : Instruction(Opcode.Eq)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }

    public record Dec() : Instruction(Opcode.Dec)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }

    public record Mul() : Instruction(Opcode.Mul)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }

    public record JNZ(string BasicBlock) : Instruction(Opcode.JNZ)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }

    public record Return() : Instruction(Opcode.Return)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };
    }
}