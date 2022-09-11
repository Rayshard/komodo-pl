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

    public abstract record Push(DataType DataType) : Instruction(Opcode.Push)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new[] { new SExpression.UnquotedSymbol(DataType.ToString()) };

        public record I64(Int64 Value) : Push(DataType.I64)
        {
            protected override IEnumerable<SExpression> OperandsAsSExpressions => base.OperandsAsSExpressions.Append(new SExpression.UnquotedSymbol(Value.ToString()));

            new public static I64 Deserialize(SExpression sexpr)
            {
                var list = sexpr.ExpectList().ExpectLength(3);
                list[0].ExpectEnum(Opcode.Push);
                list[1].ExpectEnum(DataType.I64);

                return new I64(list[2].AsInt64());
            }
        }

        public record Bool(bool Value) : Push(DataType.Bool)
        {
            protected override IEnumerable<SExpression> OperandsAsSExpressions => base.OperandsAsSExpressions.Append(new SExpression.UnquotedSymbol(Value ? "true" : "false"));

            new public static Bool Deserialize(SExpression sexpr)
            {
                var list = sexpr.ExpectList().ExpectLength(3);
                list[0].ExpectEnum(Opcode.Push);
                list[1].ExpectEnum(DataType.Bool);

                return new Bool(list[2].AsBool());
            }
        }

        new public static Push Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum(Opcode.Push);

            return list[1].AsEnum<DataType>() switch
            {
                DataType.I64 => I64.Deserialize(sexpr),
                DataType.Bool => Bool.Deserialize(sexpr),
                var dt => throw new NotImplementedException(dt.ToString())
            };
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

    public record Assert() : Instruction(Opcode.Assert)
    {
        protected override IEnumerable<SExpression> OperandsAsSExpressions => new SExpression[] { };

        new public static Assert Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Assert);

            return new Assert();
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