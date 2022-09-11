using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public enum Opcode
{
    Push,
    Syscall,
    Call,
    Return,
    LoadArg,
    CJump,
    Assert,

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

    public record Push(Value Value) : Instruction(Opcode.Push)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { Value.AsSExpression() };

        new public static Push Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Push);

            return new Push(Value.Deserialize(list[1]));
        }
    }

    public record Assert(Value Value) : Instruction(Opcode.Assert)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { Value.AsSExpression() };

        new public static Assert Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Assert);

            return new Assert(Value.Deserialize(list[1]));
        }
    }

    public record Add(DataType DataType, Value? Value = null) : Instruction(Opcode.Add)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions =>
            new[] { new SExpression.UnquotedSymbol(DataType.ToString()) }
            .AppendIf(Value is not null, Value!.AsSExpression());

        new public static Add Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, 3);
            list[0].ExpectEnum(Opcode.Add);

            return new Add(list[1].AsEnum<DataType>(), list.Count() == 3 ? Value.Deserialize(list[2]) : null);
        }
    }

    public record Print(DataType DataType) : Instruction(Opcode.Print)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(DataType.ToString()) };

        new public static Print Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Print);

            return new Print(list[1].AsEnum<DataType>());
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

    public record Eq(DataType DataType, Value? Value = null) : Instruction(Opcode.Eq)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions =>
            new[] { new SExpression.UnquotedSymbol(DataType.ToString()) }
            .AppendIf(Value is not null, Value!.AsSExpression());

        new public static Eq Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, 3);
            list[0].ExpectEnum(Opcode.Eq);

            return new Eq(list[1].AsEnum<DataType>(), list.Count() == 3 ? Value.Deserialize(list[2]) : null);
        }
    }

    public record Dec(DataType DataType) : Instruction(Opcode.Dec)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(DataType.ToString()) };

        new public static Dec Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Dec);

            return new Dec(list[1].AsEnum<DataType>());
        }
    }

    public record Mul(DataType DataType, Value? Value = null) : Instruction(Opcode.Mul)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions =>
            new[] { new SExpression.UnquotedSymbol(DataType.ToString()) }
            .AppendIf(Value is not null, Value!.AsSExpression());

        new public static Mul Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, 3);
            list[0].ExpectEnum(Opcode.Mul);

            return new Mul(list[1].AsEnum<DataType>(), list.Count() == 3 ? Value.Deserialize(list[2]) : null);
        }
    }

    public record CJump(string BasicBlock) : Instruction(Opcode.CJump)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static CJump Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.CJump);

            return new CJump(list[1].ExpectUnquotedSymbol().Value);
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
        Opcode.Push => Push.Deserialize(sexpr),
        Opcode.Add => Add.Deserialize(sexpr),
        Opcode.Syscall => Syscall.Deserialize(sexpr),
        Opcode.Print => Print.Deserialize(sexpr),
        Opcode.Call => Call.Deserialize(sexpr),
        Opcode.LoadArg => LoadArg.Deserialize(sexpr),
        Opcode.Eq => Eq.Deserialize(sexpr),
        Opcode.Dec => Dec.Deserialize(sexpr),
        Opcode.Mul => Mul.Deserialize(sexpr),
        Opcode.CJump => CJump.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),
        Opcode.Assert => Assert.Deserialize(sexpr),
        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}