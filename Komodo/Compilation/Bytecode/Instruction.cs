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

        new public static Syscall Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Syscall);

            return new Syscall(list[1].AsEnum<SyscallCode>());
        }
    }

    public record PushI64(Int64 Value) : Instruction(Opcode.PushI64)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(Value.ToString()) };

        new public static PushI64 Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.PushI64);

            return new PushI64(list[1].AsInt64());
        }
    }

    public record Add() : Instruction(Opcode.Add)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Add Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Add);

            return new Add();
        }
    }

    public record Print() : Instruction(Opcode.Print)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Print Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Print);

            return new Print();
        }
    }

    public record Call(string Module, string Function) : Instruction(Opcode.Call)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[]
        {
            new SExpression.UnquotedSymbol(Module),
            new SExpression.UnquotedSymbol(Function),
        };

        new public static Call Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum(Opcode.Call);

            return new Call(list[1].ExpectUnquotedSymbol().Value, list[2].ExpectUnquotedSymbol().Value);
        }
    }

    public record LoadArg(UInt64 Index) : Instruction(Opcode.LoadArg)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(Index.ToString()) };

        new public static LoadArg Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.LoadArg);

            return new LoadArg(list[1].AsUInt64());
        }
    }

    public record Eq() : Instruction(Opcode.Eq)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Eq Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Eq);

            return new Eq();
        }
    }

    public record Dec() : Instruction(Opcode.Dec)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Dec Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Dec);

            return new Dec();
        }
    }

    public record Mul() : Instruction(Opcode.Mul)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Mul Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Mul);

            return new Mul();
        }
    }

    public record JNZ(string BasicBlock) : Instruction(Opcode.JNZ)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static JNZ Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.JNZ);

            return new JNZ(list[1].ExpectUnquotedSymbol().Value);
        }
    }

    public record Return() : Instruction(Opcode.Return)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Return Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Return);

            return new Return();
        }
    }

    public static Instruction Deserialize(SExpression sexpr) => sexpr.ExpectList().ExpectLength(1, null)[0].AsEnum<Opcode>() switch
    {
        Opcode.PushI64 => PushI64.Deserialize(sexpr),
        Opcode.Add => Add.Deserialize(sexpr),
        Opcode.Syscall => Syscall.Deserialize(sexpr),
        Opcode.Print => Print.Deserialize(sexpr),
        Opcode.Call => Call.Deserialize(sexpr),
        Opcode.LoadArg => LoadArg.Deserialize(sexpr),
        Opcode.Eq => Eq.Deserialize(sexpr),
        Opcode.Dec => Dec.Deserialize(sexpr),
        Opcode.Mul => Mul.Deserialize(sexpr),
        Opcode.JNZ => JNZ.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),
        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}