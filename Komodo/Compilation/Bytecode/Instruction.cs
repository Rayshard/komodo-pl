using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public enum Opcode
{
    Load,
    Syscall,
    Call,
    Return,
    CJump,
    Assert,

    Add,
    Eq,
    Dec,
    Mul,

    Print,
}

public enum SyscallCode { Exit }

public abstract record Instruction(Opcode Opcode)
{
    public abstract IEnumerable<IOperand> Operands { get; }

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol(Opcode.ToString()));
        nodes.AddRange(Operands.Select(op => op.AsSExpression()));
        return new SExpression.List(nodes);
    }

    public record Syscall(SyscallCode Code) : Instruction(Opcode.Syscall)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Enumeration<SyscallCode>(Code) };

        new public static Syscall Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Syscall);

            return new Syscall(list[1].AsEnum<SyscallCode>());
        }
    }

    public record Load(Operand.Source Source) : Instruction(Opcode.Load)
    {
        public override IEnumerable<IOperand> Operands => new[] { Source };

        new public static Load Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Load);

            return new Load(Operand.DeserializeSource(list[1]));
        }
    }

    public record Assert(Value Value) : Instruction(Opcode.Assert)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Constant(Value) };

        new public static Assert Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Assert);

            return new Assert(Value.Deserialize(list[1]));
        }
    }

    public record Binop(Opcode Opcode, DataType DataType, Value? Value = null) : Instruction(Verify(Opcode))
    {
        public override IEnumerable<IOperand> Operands =>
            new[] { new Operand.Enumeration<DataType>(DataType) }
            .AppendIf<Operand>(Value is not null, new Operand.Constant(Value!));

        public static Opcode Verify(Opcode opcode) => opcode switch
        {
            Opcode.Add or Opcode.Mul or Opcode.Dec or Opcode.Eq => opcode,
            _ => throw new ArgumentException($"'{opcode}' is not a binop!")
        };

        new public static Binop Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, 3);
            return new Binop(list[0].AsEnum<Opcode>(), list[1].AsEnum<DataType>(), list.Count() == 3 ? Value.Deserialize(list[2]) : null);
        }
    }

    public record Print(DataType DataType) : Instruction(Opcode.Print)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Enumeration<DataType>(DataType) };
        
        new public static Print Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Print);

            return new Print(list[1].AsEnum<DataType>());
        }
    }

    public record Call(string Module, string Function) : Instruction(Opcode.Call)
    {
        public override IEnumerable<IOperand> Operands => new[]
        {
            new Operand.Identifier(Module),
            new Operand.Identifier(Function),
        };

        new public static Call Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum(Opcode.Call);

            return new Call(list[1].ExpectUnquotedSymbol().Value, list[2].ExpectUnquotedSymbol().Value);
        }
    }

    public record Dec(DataType DataType) : Instruction(Opcode.Dec)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Enumeration<DataType>(DataType) };

        new public static Dec Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.Dec);

            return new Dec(list[1].AsEnum<DataType>());
        }
    }

    public record CJump(string BasicBlock) : Instruction(Opcode.CJump)
    {
        public override IEnumerable<IOperand> Operands => new Operand[] { new Operand.Identifier(BasicBlock) };
        
        new public static CJump Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum(Opcode.CJump);

            return new CJump(list[1].ExpectUnquotedSymbol().Value);
        }
    }

    public record Return() : Instruction(Opcode.Return)
    {
        public override IEnumerable<IOperand> Operands => new Operand[] { };

        new public static Return Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1);
            list[0].ExpectEnum(Opcode.Return);

            return new Return();
        }
    }

    public static Instruction Deserialize(SExpression sexpr) => sexpr.ExpectList().ExpectLength(1, null)[0].AsEnum<Opcode>() switch
    {
        Opcode.Load => Load.Deserialize(sexpr),
        Opcode.Add => Binop.Deserialize(sexpr),
        Opcode.Syscall => Syscall.Deserialize(sexpr),
        Opcode.Print => Print.Deserialize(sexpr),
        Opcode.Call => Call.Deserialize(sexpr),
        Opcode.Eq => Binop.Deserialize(sexpr),
        Opcode.Dec => Dec.Deserialize(sexpr),
        Opcode.Mul => Binop.Deserialize(sexpr),
        Opcode.CJump => CJump.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),
        Opcode.Assert => Assert.Deserialize(sexpr),
        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}