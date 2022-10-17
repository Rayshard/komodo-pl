using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public enum Opcode
{
    LoadConstant,
    LoadGlobal,
    StoreGlobal,
    LoadLocal,
    StoreLocal,
    LoadMem,
    StoreMem,
    LoadArg,
    Assert,
    Return,
    Allocate,
    Convert,
    Duplicate,
    Rotate2,

    Jump,
    Exit,

    Call,


    Add,
    Eq,
    Dec,
    Mul,
    LShift,
    BOR,

    Reinterpret,
    ZeroExtend,

    Dump,


    // Array specific instructions
    GetElement,
    GetLength,
}

public enum Comparison { EQ, NEQ };

public abstract record Instruction(Opcode Opcode) : FunctionBodyElement
{
    public virtual IEnumerable<IOperand> Operands => new IOperand[0];

    public virtual SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol(Opcode.ToString()));
        nodes.AddRange(Operands.Select(op => op.AsSExpression()));
        return new SExpression.List(nodes);
    }

    private static T Deserialize<T>(SExpression sexpr, Opcode opcode, Func<T> constructor)
    {
        sexpr.ExpectList()
             .ExpectLength(1)
             .ExpectItem(0, item => item.ExpectEnum<Opcode>(opcode));

        return constructor();
    }

    private static T Deserialize<T, TOperand>(
        SExpression sexpr,
        Opcode opcode,
        Func<SExpression, TOperand> operandDeserializer,
        Func<TOperand, T> constructor
    )
    {
        sexpr.ExpectList()
             .ExpectLength(2)
             .ExpectItem(0, item => item.ExpectEnum<Opcode>(opcode))
             .ExpectItem(1, operandDeserializer, out var operand);

        return constructor(operand);
    }

    private static T Deserialize<T, TOperand1, TOperand2>(
        SExpression sexpr,
        Opcode opcode,
        Func<SExpression, TOperand1> operand1Deserializer,
        Func<SExpression, TOperand2> operand2Deserializer,
        Func<TOperand1, TOperand2, T> constructor
    )
    {
        sexpr.ExpectList()
             .ExpectLength(3)
             .ExpectItem(0, item => item.ExpectEnum<Opcode>(opcode))
             .ExpectItem(1, operand1Deserializer, out var operand1)
             .ExpectItem(2, operand2Deserializer, out var operand2);

        return constructor(operand1, operand2);
    }

    public record Duplicate() : Instruction(Opcode.Duplicate)
    {
        new public static Duplicate Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Duplicate, delegate { return new Duplicate(); });
    }

    public record Rotate2() : Instruction(Opcode.Rotate2)
    {
        new public static Rotate2 Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Rotate2, delegate { return new Rotate2(); });
    }

    public record LoadConstant(Operand.Constant Constant) : Instruction(Opcode.LoadConstant)
    {
        public override IEnumerable<IOperand> Operands => new[] { Constant };

        new public static LoadConstant Deserialize(SExpression sexpr)
            => Deserialize(sexpr, Opcode.LoadConstant, Operand.Constant.Deserialize, constant => new LoadConstant(constant));
    }

    public record LoadGlobal(string ModuleName, string GlobalName) : Instruction(Opcode.LoadGlobal)
    {
        public override IEnumerable<IOperand> Operands => new[] {
            new Operand.Identifier(ModuleName),
            new Operand.Identifier(GlobalName)
        };

        new public static LoadGlobal Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.LoadGlobal,
            Operand.Identifier.Deserialize,
            Operand.Identifier.Deserialize,
            (moduleName, globalName) => new LoadGlobal(moduleName.Value, globalName.Value)
        );
    }

    public record StoreGlobal(string ModuleName, string GlobalName) : Instruction(Opcode.StoreGlobal)
    {
        public override IEnumerable<IOperand> Operands => new[] {
            new Operand.Identifier(ModuleName),
            new Operand.Identifier(GlobalName)
        };

        new public static StoreGlobal Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.StoreGlobal,
            Operand.Identifier.Deserialize,
            Operand.Identifier.Deserialize,
            (moduleName, globalName) => new StoreGlobal(moduleName.Value, globalName.Value)
        );
    }

    public record LoadLocal(string Name) : Instruction(Opcode.LoadLocal)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Name) };

        new public static LoadLocal Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.LoadLocal,
            Operand.Identifier.Deserialize,
            name => new LoadLocal(name.Value)
        );
    }

    public record StoreLocal(string Name) : Instruction(Opcode.StoreLocal)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Name) };

        new public static StoreLocal Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.StoreLocal,
            Operand.Identifier.Deserialize,
            name => new StoreLocal(name.Value)
        );
    }

    public record LoadArg(string Name) : Instruction(Opcode.LoadArg)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Name) };

        new public static LoadArg Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.LoadArg,
            Operand.Identifier.Deserialize,
            name => new LoadArg(name.Value)
        );
    }

    public record Return() : Instruction(Opcode.Return)
    {
        new public static Return Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Return, delegate { return new Return(); });
    }

    public record Assert(Comparison Comparison) : Instruction(Opcode.Assert)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Enumeration<Comparison>(Comparison) };

        new public static Assert Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.Assert,
            Operand.Enumeration<Comparison>.Deserialize,
            comparison => new Assert(comparison.Value)
        );
    }

    public record Allocate(DataType? DataType = null) : Instruction(Opcode.Allocate)
    {
        public override IEnumerable<IOperand> Operands => DataType is null
            ? new IOperand[0]
            : new IOperand[] { new Operand.DataType(DataType) };

        new public static Allocate Deserialize(SExpression sexpr)
        {
            try { return Deserialize(sexpr, Opcode.Allocate, delegate { return new Allocate(); }); }
            catch (SExpression.FormatException) { }

            try { return Deserialize(sexpr, Opcode.Allocate, DataType.Deserialize, dt => new Allocate(dt)); }
            catch (SExpression.FormatException) { }

            throw new SExpression.FormatException($"Invalid Allocate instruction: {sexpr}", sexpr);
        }
    }
    
    public record Convert(DataType Target) : Instruction(Opcode.Convert)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.DataType(Target) };

        new public static Convert Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.Convert,
            DataType.Deserialize,
            dataType => new Convert(dataType)
        );
    }

    public record LoadMem(DataType? DataType = null) : Instruction(Opcode.LoadMem)
    {
        public override IEnumerable<IOperand> Operands => DataType is null
            ? new IOperand[0]
            : new IOperand[] { new Operand.DataType(DataType) };

        new public static LoadMem Deserialize(SExpression sexpr)
        {
            try { return Deserialize(sexpr, Opcode.LoadMem, delegate { return new LoadMem(); }); }
            catch (SExpression.FormatException) { }

            try { return Deserialize(sexpr, Opcode.LoadMem, DataType.Deserialize, dt => new LoadMem(dt)); }
            catch (SExpression.FormatException) { }

            throw new SExpression.FormatException($"Invalid LoadMem instruction: {sexpr}", sexpr);
        }
    }

    public record StoreMem() : Instruction(Opcode.StoreMem)
    {
        new public static StoreMem Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.StoreMem, delegate { return new StoreMem(); });
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
            catch (SExpression.FormatException) { }

            try { return Indirect.Deserialize(sexpr); }
            catch (SExpression.FormatException) { }

            throw new SExpression.FormatException($"Invalid Call instruction: {sexpr}", sexpr);
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
        Opcode.LoadConstant => LoadConstant.Deserialize(sexpr),
        Opcode.LoadGlobal => LoadGlobal.Deserialize(sexpr),
        Opcode.StoreGlobal => StoreGlobal.Deserialize(sexpr),
        Opcode.LoadLocal => LoadLocal.Deserialize(sexpr),
        Opcode.StoreLocal => StoreLocal.Deserialize(sexpr),
        Opcode.LoadMem => LoadMem.Deserialize(sexpr),
        Opcode.StoreMem => StoreMem.Deserialize(sexpr),
        Opcode.LoadArg => LoadArg.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),
        Opcode.Assert => Assert.Deserialize(sexpr),
        Opcode.Duplicate => Duplicate.Deserialize(sexpr),
        Opcode.Convert => Convert.Deserialize(sexpr),
        Opcode.Allocate => Allocate.Deserialize(sexpr),
        Opcode.Rotate2 => Rotate2.Deserialize(sexpr),


        Opcode.Add => Binop.Add.Deserialize(sexpr),
        Opcode.Dump => Dump.Deserialize(sexpr),
        Opcode.Eq => Binop.Eq.Deserialize(sexpr),
        Opcode.Mul => Binop.Mul.Deserialize(sexpr),
        Opcode.LShift => Binop.LShift.Deserialize(sexpr),
        Opcode.BOR => Binop.BOR.Deserialize(sexpr),
        Opcode.Dec => Unop.Dec.Deserialize(sexpr),

        Opcode.Jump => Jump.Deserialize(sexpr),

        Opcode.GetElement => Binop.GetElement.Deserialize(sexpr),
        Opcode.GetLength => Unop.GetLength.Deserialize(sexpr),

        Opcode.Call => Call.Deserialize(sexpr),


        Opcode.Reinterpret => Reinterpret.Deserialize(sexpr),
        Opcode.ZeroExtend => ZeroExtend.Deserialize(sexpr),

        Opcode.Exit => Exit.Deserialize(sexpr),


        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}