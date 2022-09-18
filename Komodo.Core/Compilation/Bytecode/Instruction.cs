using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public enum Opcode
{
    Load,
    Store,
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
    GetElement,
}

public abstract record Instruction(Opcode Opcode) : FunctionBodyElement
{
    public abstract IEnumerable<IOperand> Operands { get; }

    public virtual SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol(Opcode.ToString()));
        nodes.AddRange(Operands.Select(op => op.AsSExpression()));
        return new SExpression.List(nodes);
    }

    public record Syscall(string Name) : Instruction(Opcode.Syscall)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Name) };

        new public static Syscall Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum<Opcode>(Opcode.Syscall);

            return new Syscall(list[1].ExpectUnquotedSymbol().Value);
        }
    }

    public record Load(Operand.Source Source) : Instruction(Opcode.Load)
    {
        public override IEnumerable<IOperand> Operands => new[] { Source };

        new public static Load Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectEnum<Opcode>(Opcode.Load);

            return new Load(Operand.DeserializeSource(list[1]));
        }
    }

    public record Store(Operand.Source Source, Operand.Destination Destination) : Instruction(Opcode.Store)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source, Destination };

        new public static Store Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum<Opcode>(Opcode.Store);

            return new Store(
                Operand.DeserializeSource(list[1]),
                Operand.DeserializeDestination(list[2])
            );
        }
    }

    public record Assert(Operand.Source Source1, Operand.Source Source2) : Instruction(Opcode.Assert)
    {
        public override IEnumerable<IOperand> Operands => new[] { Source1, Source2 };

        new public static Assert Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum<Opcode>(Opcode.Assert);

            return new Assert(
                Operand.DeserializeSource(list[1]),
                Operand.DeserializeSource(list[2])
            );
        }
    }

    public record Binop(Opcode Opcode, DataType DataType, Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination) : Instruction(Verify(Opcode))
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] {
            new Operand.DataType(DataType),
            Source1,
            Source2,
            Destination
        };

        public static Opcode Verify(Opcode opcode) => opcode switch
        {
            Opcode.Add or Opcode.Mul or Opcode.Dec or Opcode.Eq or
            Opcode.GetElement => opcode,
            _ => throw new ArgumentException($"'{opcode}' is not a binop!")
        };

        new public static Binop Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(5);

            return new Binop(
                list[0].ExpectEnum<Opcode>(),
                list[1].Expect(DataType.Deserialize),
                Operand.DeserializeSource(list[2]),
                Operand.DeserializeSource(list[3]),
                Operand.DeserializeDestination(list[4])
            );
        }
    }

    public record Print(DataType DataType, Operand.Source Source) : Instruction(Opcode.Print)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.DataType(DataType), Source };

        new public static Print Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum<Opcode>(Opcode.Print);

            return new Print(
                list[1].Expect(DataType.Deserialize),
                Operand.DeserializeSource(list[2])
            );
        }
    }

    public record Call(string Module, string Function, IEnumerable<Operand.Source> Args, IEnumerable<Operand.Destination> Returns) : Instruction(Opcode.Call)
    {
        private static readonly SExpression ArgsReturnsDivider = new SExpression.UnquotedSymbol("~");

        public override IEnumerable<IOperand> Operands
        {
            get
            {
                var operands = new List<IOperand>();
                operands.Add(new Operand.Identifier(Module));
                operands.Add(new Operand.Identifier(Function));
                operands.AddRange(Args);
                operands.AddRange(Returns);
                return operands;
            }
        }

        public override SExpression AsSExpression()
        {
            var items = base.AsSExpression().ExpectList().ToList();
            items.Insert(3 + Args.Count(), ArgsReturnsDivider);
            return new SExpression.List(items);
        } 

        new public static Call Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3, null);
            list[0].ExpectEnum<Opcode>(Opcode.Call);

            var args = list.Skip(3).TakeWhile(item => !item.Matches(ArgsReturnsDivider)).Select(Operand.DeserializeSource).ToList();
            var returns = list.Skip(3).Skip(args.Count).Skip(1).Select(Operand.DeserializeDestination).ToList();

            return new Call(
                list[1].ExpectUnquotedSymbol().Value,
                list[2].ExpectUnquotedSymbol().Value,
                args,
                returns
            );
        }
    }

    public record Dec(DataType DataType, Operand.Source Source, Operand.Destination Destination) : Instruction(Opcode.Dec)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] {
            new Operand.DataType(DataType),
            Source,
            Destination
        };

        new public static Dec Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(4);
            list[0].ExpectEnum<Opcode>(Opcode.Dec);

            return new Dec(
                list[1].Expect(DataType.Deserialize),
                Operand.DeserializeSource(list[2]),
                Operand.DeserializeDestination(list[3])
            );
        }
    }

    public record CJump(string Label, Operand.Source Condtion) : Instruction(Opcode.CJump)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.Identifier(Label), Condtion };

        new public static CJump Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum<Opcode>(Opcode.CJump);

            return new CJump(
                list[1].ExpectUnquotedSymbol().Value,
                Operand.DeserializeSource(list[2])
            );
        }
    }

    public record Return(IEnumerable<Operand.Source> Sources) : Instruction(Opcode.Return)
    {
        public override IEnumerable<IOperand> Operands => Sources;

        new public static Return Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(1, null);
            list[0].ExpectEnum<Opcode>(Opcode.Return);

            return new Return(list.Skip(1).Select(Operand.DeserializeSource));
        }
    }

    public static Instruction Deserialize(SExpression sexpr) => sexpr.ExpectList().ExpectLength(1, null)[0].ExpectEnum<Opcode>() switch
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
        Opcode.Store => Store.Deserialize(sexpr),
        Opcode.GetElement => Binop.Deserialize(sexpr),
        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}