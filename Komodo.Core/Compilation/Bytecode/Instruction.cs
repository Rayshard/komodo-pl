using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public enum Opcode
{
    Move,
    Return,
    Jump,
    Assert,
    Exit,

    Call,

    Allocate,
    IsNull,
    Load,
    Store,

    Add,
    Eq,
    Dec,
    Mul,
    LShift,
    BOR,

    Convert,
    Reinterpret,
    ZeroExtend,

    Dump,

    // Array specific instructions
    GetElement,
    GetLength,
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

    public record Move(Operand.Source Source, Operand.Destination Destination) : Instruction(Opcode.Move)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source, Destination };

        new public static Move Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(3);
            list[0].ExpectEnum<Opcode>(Opcode.Move);

            return new Move(
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

    public record Exit(Operand.Source Code) : Instruction(Opcode.Exit)
    {
        public override IEnumerable<IOperand> Operands => new[] { Code };

        new public static Exit Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Exit))
                 .ExpectItem(1, Operand.DeserializeSource, out var code);

            return new Exit(code);
        }
    }

    public abstract record Binop(Opcode Opcode, Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination) : Instruction(Opcode)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source1, Source2, Destination };

        private static void Deserialize(
            SExpression sexpr,
            Opcode expected,
            out Operand.Source source1,
            out Operand.Source source2,
            out Operand.Destination destination
        )
        {
            sexpr.ExpectList()
                 .ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(expected))
                 .ExpectItem(1, Operand.DeserializeSource, out source1)
                 .ExpectItem(2, Operand.DeserializeSource, out source2)
                 .ExpectItem(3, Operand.DeserializeDestination, out destination);
        }

        public record Add(Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination)
            : Binop(Opcode.Add, Source1, Source2, Destination)
        {
            new public static Add Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.Add, out var source1, out var source2, out var destination);
                return new Add(source1, source2, destination);
            }
        }

        public record Mul(Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination)
            : Binop(Opcode.Mul, Source1, Source2, Destination)
        {
            new public static Mul Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.Mul, out var source1, out var source2, out var destination);
                return new Mul(source1, source2, destination);
            }
        }

        public record Eq(Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination)
            : Binop(Opcode.Eq, Source1, Source2, Destination)
        {
            new public static Eq Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.Eq, out var source1, out var source2, out var destination);
                return new Eq(source1, source2, destination);
            }
        }

        public record GetElement(Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination)
            : Binop(Opcode.GetElement, Source1, Source2, Destination)
        {
            new public static GetElement Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.GetElement, out var source1, out var source2, out var destination);
                return new GetElement(source1, source2, destination);
            }
        }

        public record LShift(Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination)
            : Binop(Opcode.LShift, Source1, Source2, Destination)
        {
            new public static LShift Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.LShift, out var source1, out var source2, out var destination);
                return new LShift(source1, source2, destination);
            }
        }

        public record BOR(Operand.Source Source1, Operand.Source Source2, Operand.Destination Destination)
            : Binop(Opcode.BOR, Source1, Source2, Destination)
        {
            new public static BOR Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.BOR, out var source1, out var source2, out var destination);
                return new BOR(source1, source2, destination);
            }
        }
    }

    public abstract record Unop(Opcode Opcode, Operand.Source Source, Operand.Destination Destination) : Instruction(Opcode)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source, Destination };

        private static void Deserialize(
            SExpression sexpr,
            Opcode expected,
            out Operand.Source source,
            out Operand.Destination destination
        )
        {
            sexpr.ExpectList()
                 .ExpectLength(3)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(expected))
                 .ExpectItem(1, Operand.DeserializeSource, out source)
                 .ExpectItem(2, Operand.DeserializeDestination, out destination);
        }

        public record Dec(Operand.Source Source, Operand.Destination Destination)
            : Unop(Opcode.Dec, Source, Destination)
        {
            new public static Dec Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.Dec, out var source, out var destination);
                return new Dec(source, destination);
            }
        }

        public record GetLength(Operand.Source Source, Operand.Destination Destination)
            : Unop(Opcode.GetLength, Source, Destination)
        {
            new public static GetLength Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.GetLength, out var source, out var destination);
                return new GetLength(source, destination);
            }
        }

        public record IsNull(Operand.Source Source, Operand.Destination Destination)
                    : Unop(Opcode.IsNull, Source, Destination)
        {
            new public static IsNull Deserialize(SExpression sexpr)
            {
                Deserialize(sexpr, Opcode.IsNull, out var source, out var destination);
                return new IsNull(source, destination);
            }
        }
    }

    public record Dump(Operand.Source Source) : Instruction(Opcode.Dump)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source };

        new public static Dump Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Dump))
                 .ExpectItem(1, Operand.DeserializeSource, out var source);

            return new Dump(source);
        }
    }

    public abstract record Call(VSROCollection<Operand.Source> Args) : Instruction(Opcode.Call)
    {
        public record Direct(string Module, string Function, VSROCollection<Operand.Source> Args) : Call(Args)
        {
            public override IEnumerable<IOperand> Operands
            {
                get
                {
                    var operands = new List<IOperand>();
                    operands.Add(new Operand.Identifier(Module));
                    operands.Add(new Operand.Identifier(Function));
                    operands.AddRange(Args);
                    return operands;
                }
            }

            new public static Direct Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(3, null)
                     .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Call))
                     .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var module)
                     .ExpectItem(2, item => item.ExpectUnquotedSymbol().Value, out var function)
                     .ExpectItems(Operand.DeserializeSource, out var args, 3);

                return new Direct(module, function, args.ToVSROCollection());
            }
        }

        public record Indirect(Operand.Source Source, VSROCollection<Operand.Source> Args) : Call(Args)
        {
            public override IEnumerable<IOperand> Operands
            {
                get
                {
                    var operands = new List<IOperand>();
                    operands.Add(Source);
                    operands.AddRange(Args);
                    return operands;
                }
            }

            new public static Indirect Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2, null)
                     .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Call))
                     .ExpectItem(1, Operand.DeserializeSource, out var source)
                     .ExpectItems(Operand.DeserializeSource, out var args, 2);

                return new Indirect(source, args.ToVSROCollection());
            }
        }

        new public static Call Deserialize(SExpression sexpr)
        {
            try { return Direct.Deserialize(sexpr); }
            catch { return Indirect.Deserialize(sexpr); }
        }
    }

    public record Jump(string Label, Operand.Source? Condition) : Instruction(Opcode.Jump)
    {
        public override IEnumerable<IOperand> Operands
            => Condition is null
            ? new IOperand[] { new Operand.Identifier(Label) }
            : new IOperand[] { new Operand.Identifier(Label), Condition };

        new public static Jump Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList()
                            .ExpectLength(2, 3, out var listLength)
                            .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Jump))
                            .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var label);

            var condition = listLength == 3 ? Operand.DeserializeSource(list[2]) : null;

            return new Jump(label, condition);
        }
    }

    public record Return(VSROCollection<Operand.Source> Sources) : Instruction(Opcode.Return)
    {
        public override IEnumerable<IOperand> Operands => Sources;

        new public static Return Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(1, null)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Return))
                 .ExpectItems(Operand.DeserializeSource, out var sources, 1);

            return new Return(sources.ToVSROCollection());
        }
    }

    public record Allocate(DataType DataType, Operand.Destination Destination) : Instruction(Opcode.Allocate)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.DataType(DataType), Destination };

        new public static Allocate Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(3)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Allocate))
                 .ExpectItem(1, DataType.Deserialize, out var dataType)
                 .ExpectItem(2, Operand.DeserializeDestination, out var destination);

            return new Allocate(dataType, destination);
        }
    }

    public record Load(Operand.Source Reference, Operand.Destination Destination) : Instruction(Opcode.Load)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Reference, Destination };

        new public static Load Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(3)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Load))
                 .ExpectItem(1, Operand.DeserializeSource, out var reference)
                 .ExpectItem(2, Operand.DeserializeDestination, out var destination);

            return new Load(reference, destination);
        }
    }

    public record Store(Operand.Source Value, Operand.Source Reference) : Instruction(Opcode.Store)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Value, Reference };

        new public static Store Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(3)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Store))
                 .ExpectItem(1, Operand.DeserializeSource, out var value)
                 .ExpectItem(2, Operand.DeserializeSource, out var reference);

            return new Store(value, reference);
        }
    }

    public record Convert(Operand.Source Source, DataType Target, Operand.Destination Destination) : Instruction(Opcode.Convert)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source, new Operand.DataType(Target), Destination };

        new public static Convert Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Convert))
                 .ExpectItem(1, Operand.DeserializeSource, out var source)
                 .ExpectItem(2, DataType.Deserialize, out var target)
                 .ExpectItem(3, Operand.DeserializeDestination, out var destination);

            return new Convert(source, target, destination);
        }
    }

    public record Reinterpret(Operand.Source Source, DataType Target, Operand.Destination Destination) : Instruction(Opcode.Reinterpret)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source, new Operand.DataType(Target), Destination };

        new public static Reinterpret Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Reinterpret))
                 .ExpectItem(1, Operand.DeserializeSource, out var source)
                 .ExpectItem(2, DataType.Deserialize, out var target)
                 .ExpectItem(3, Operand.DeserializeDestination, out var destination);

            return new Reinterpret(source, target, destination);
        }
    }

    public record ZeroExtend(Operand.Source Source, DataType Target, Operand.Destination Destination) : Instruction(Opcode.ZeroExtend)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Source, new Operand.DataType(Target), Destination };

        new public static ZeroExtend Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.ZeroExtend))
                 .ExpectItem(1, Operand.DeserializeSource, out var source)
                 .ExpectItem(2, DataType.Deserialize, out var target)
                 .ExpectItem(3, Operand.DeserializeDestination, out var destination);

            return new ZeroExtend(source, target, destination);
        }
    }

    public static Instruction Deserialize(SExpression sexpr) => sexpr.ExpectList().ExpectLength(1, null)[0].ExpectEnum<Opcode>() switch
    {
        Opcode.Move => Unop.Move.Deserialize(sexpr),

        Opcode.Add => Binop.Add.Deserialize(sexpr),
        Opcode.Dump => Dump.Deserialize(sexpr),
        Opcode.Eq => Binop.Eq.Deserialize(sexpr),
        Opcode.Mul => Binop.Mul.Deserialize(sexpr),
        Opcode.LShift => Binop.LShift.Deserialize(sexpr),
        Opcode.BOR => Binop.BOR.Deserialize(sexpr),
        Opcode.Dec => Unop.Dec.Deserialize(sexpr),

        Opcode.Jump => Jump.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),

        Opcode.GetElement => Binop.GetElement.Deserialize(sexpr),
        Opcode.GetLength => Unop.GetLength.Deserialize(sexpr),

        Opcode.Call => Call.Deserialize(sexpr),

        Opcode.Allocate => Allocate.Deserialize(sexpr),
        Opcode.IsNull => Unop.IsNull.Deserialize(sexpr),
        Opcode.Load => Load.Deserialize(sexpr),
        Opcode.Store => Store.Deserialize(sexpr),

        Opcode.Convert => Convert.Deserialize(sexpr),
        Opcode.Reinterpret => Reinterpret.Deserialize(sexpr),
        Opcode.ZeroExtend => ZeroExtend.Deserialize(sexpr),

        Opcode.Assert => Assert.Deserialize(sexpr),
        Opcode.Exit => Exit.Deserialize(sexpr),

        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}