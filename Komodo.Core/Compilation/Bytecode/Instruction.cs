using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public enum Opcode
{
    Move,
    Syscall,
    Call,
    Return,
    Jump,
    Assert,
    Exit,

    Allocate,
    Load,
    Store,

    Convert,
    Reinterpret,

    Add,
    Eq,
    Dec,
    Mul,

    Dump,
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
            Opcode.Add or Opcode.Mul or Opcode.Eq or
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

    public record Unop(Opcode Opcode, DataType DataType, Operand.Source Source, Operand.Destination Destination) : Instruction(Verify(Opcode))
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] {
            new Operand.DataType(DataType),
            Source,
            Destination
        };

        public static Opcode Verify(Opcode opcode) => opcode switch
        {
            Opcode.Dec => opcode,
            _ => throw new ArgumentException($"'{opcode}' is not a unop!")
        };

        new public static Unop Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList().ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(), out var opcode)
                 .ExpectItem(1, DataType.Deserialize, out var dataType)
                 .ExpectItem(2, Operand.DeserializeSource, out var source)
                 .ExpectItem(3, Operand.DeserializeDestination, out var destination);

            return new Unop(opcode, dataType, source, destination);
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

    public record Call(string Module, string Function, VSROCollection<Operand.Source> Args) : Instruction(Opcode.Call)
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

        new public static Call Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(3, null)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Call))
                 .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var module)
                 .ExpectItem(2, item => item.ExpectUnquotedSymbol().Value, out var function)
                 .ExpectItems(Operand.DeserializeSource, out var args, 3);

            return new Call(module, function, new VSROCollection<Operand.Source>(args));
        }
    }

    public record Syscall(string Name) : Instruction(Opcode.Syscall)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.Identifier(Name) };

        new public static Syscall Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Syscall))
                 .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name);

            return new Syscall(name);
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

            return new Return(new VSROCollection<Operand.Source>(sources));
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

    public record Convert(Operand.Source Value, DataType Target, Operand.Destination Destination) : Instruction(Opcode.Convert)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Value, new Operand.DataType(Target), Destination };

        new public static Convert Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Convert))
                 .ExpectItem(1, Operand.DeserializeSource, out var value)
                 .ExpectItem(2, DataType.Deserialize, out var target)
                 .ExpectItem(3, Operand.DeserializeDestination, out var destination);

            return new Convert(value, target, destination);
        }
    }

public record Reinterpret(Operand.Source Value, DataType Target, Operand.Destination Destination) : Instruction(Opcode.Reinterpret)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { Value, new Operand.DataType(Target), Destination };

        new public static Reinterpret Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(4)
                 .ExpectItem(0, item => item.ExpectEnum<Opcode>(Opcode.Reinterpret))
                 .ExpectItem(1, Operand.DeserializeSource, out var value)
                 .ExpectItem(2, DataType.Deserialize, out var target)
                 .ExpectItem(3, Operand.DeserializeDestination, out var destination);

            return new Reinterpret(value, target, destination);
        }
    }

    public static Instruction Deserialize(SExpression sexpr) => sexpr.ExpectList().ExpectLength(1, null)[0].ExpectEnum<Opcode>() switch
    {
        Opcode.Add => Binop.Deserialize(sexpr),
        Opcode.Syscall => Syscall.Deserialize(sexpr),
        Opcode.Dump => Dump.Deserialize(sexpr),
        Opcode.Call => Call.Deserialize(sexpr),
        Opcode.Eq => Binop.Deserialize(sexpr),
        Opcode.Dec => Unop.Deserialize(sexpr),
        Opcode.Mul => Binop.Deserialize(sexpr),
        Opcode.Jump => Jump.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),
        Opcode.Assert => Assert.Deserialize(sexpr),
        Opcode.Exit => Exit.Deserialize(sexpr),
        Opcode.Move => Move.Deserialize(sexpr),
        Opcode.GetElement => Binop.Deserialize(sexpr),
        
        Opcode.Allocate => Allocate.Deserialize(sexpr),
        Opcode.Load => Load.Deserialize(sexpr),
        Opcode.Store => Store.Deserialize(sexpr),

        Opcode.Convert => Convert.Deserialize(sexpr),
        Opcode.Reinterpret => Reinterpret.Deserialize(sexpr),
      
        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}