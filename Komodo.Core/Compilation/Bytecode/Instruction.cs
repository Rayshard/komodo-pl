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
    LoadData,
    LoadFunction,
    Assert,
    Return,
    Allocate,
    AllocateArray,
    Convert,
    Duplicate,
    Rotate2,
    Dump,
    Reinterpret,
    Jump,
    CJump,
    ZeroExtend,
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Compare,
    LShift,
    Or,
    And,
    Xor,
    Exit,
    Dec,
    Inc,
    LoadElement,
    StoreElement,
    LoadLength,
    Call,
    Syscall,
    LoadSysfunc,
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

    public record LoadData(string ModuleName, string DataName) : Instruction(Opcode.LoadData)
    {
        public override IEnumerable<IOperand> Operands => new[] {
            new Operand.Identifier(ModuleName),
            new Operand.Identifier(DataName)
        };

        new public static LoadData Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.LoadData,
            Operand.Identifier.Deserialize,
            Operand.Identifier.Deserialize,
            (moduleName, dataName) => new LoadData(moduleName.Value, dataName.Value)
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

            throw new SExpression.FormatException($"Invalid LoadMem instruction: {sexpr}", sexpr);
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

    public record Dump() : Instruction(Opcode.Dump)
    {
        new public static Dump Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Dump, delegate { return new Dump(); });
    }

    public record Reinterpret(DataType Target) : Instruction(Opcode.Reinterpret)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.DataType(Target) };

        new public static Reinterpret Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.Reinterpret,
            DataType.Deserialize,
            dataType => new Reinterpret(dataType)
        );
    }

    public record Jump(string Label) : Instruction(Opcode.Jump)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Label) };

        new public static Jump Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.Jump,
            Operand.Identifier.Deserialize,
            label => new Jump(label.Value)
        );
    }

    public record CJump(string Label) : Instruction(Opcode.Jump)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Label) };

        new public static CJump Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.CJump,
            Operand.Identifier.Deserialize,
            label => new CJump(label.Value)
        );
    }

    public record ZeroExtend(DataType Target) : Instruction(Opcode.ZeroExtend)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.DataType(Target) };

        new public static ZeroExtend Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.ZeroExtend,
            DataType.Deserialize,
            dataType => new ZeroExtend(dataType)
        );
    }

    public record Exit() : Instruction(Opcode.Exit)
    {
        new public static Exit Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Exit, delegate { return new Exit(); });
    }

    public record Add() : Instruction(Opcode.Add)
    {
        new public static Add Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Add, delegate { return new Add(); });
    }

    public record Sub() : Instruction(Opcode.Sub)
    {
        new public static Sub Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Sub, delegate { return new Sub(); });
    }

    public record Mul() : Instruction(Opcode.Mul)
    {
        new public static Mul Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Mul, delegate { return new Mul(); });
    }

    public record Div() : Instruction(Opcode.Div)
    {
        new public static Div Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Div, delegate { return new Div(); });
    }

    public record Mod() : Instruction(Opcode.Mod)
    {
        new public static Mod Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Mod, delegate { return new Mod(); });
    }

    public record Or() : Instruction(Opcode.Or)
    {
        new public static Or Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Or, delegate { return new Or(); });
    }

    public record And() : Instruction(Opcode.And)
    {
        new public static And Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.And, delegate { return new And(); });
    }

    public record Xor() : Instruction(Opcode.Xor)
    {
        new public static Xor Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Xor, delegate { return new Xor(); });
    }

    public record LShift() : Instruction(Opcode.LShift)
    {
        new public static LShift Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.LShift, delegate { return new LShift(); });
    }

    public record Dec() : Instruction(Opcode.Dec)
    {
        new public static Dec Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Dec, delegate { return new Dec(); });
    }

    public record Inc() : Instruction(Opcode.Inc)
    {
        new public static Inc Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Inc, delegate { return new Inc(); });
    }

    public record LoadElement() : Instruction(Opcode.LoadElement)
    {
        new public static LoadElement Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.LoadElement, delegate { return new LoadElement(); });
    }

    public record StoreElement() : Instruction(Opcode.StoreElement)
    {
        new public static StoreElement Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.StoreElement, delegate { return new StoreElement(); });
    }

    public record LoadLength() : Instruction(Opcode.LoadLength)
    {
        new public static LoadLength Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.LoadLength, delegate { return new LoadLength(); });
    }

    public record Compare(Comparison Comparison) : Instruction(Opcode.Compare)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Enumeration<Comparison>(Comparison) };

        new public static Compare Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.Compare,
            Operand.Enumeration<Comparison>.Deserialize,
            comparison => new Compare(comparison.Value)
        );
    }

    public abstract record Call() : Instruction(Opcode.Call)
    {
        public record Direct(string Module, string Function) : Call
        {
            public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Module), new Operand.Identifier(Function) };

            new public static Direct Deserialize(SExpression sexpr) => Deserialize(
                sexpr,
                Opcode.Call,
                Operand.Identifier.Deserialize,
                Operand.Identifier.Deserialize,
                (moduleName, functionName) => new Direct(moduleName.Value, functionName.Value)
            );
        }

        public record Indirect() : Call
        {
            new public static Indirect Deserialize(SExpression sexpr) => Deserialize(sexpr, Opcode.Call, delegate { return new Indirect(); });
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

    public record Syscall(string Name) : Instruction(Opcode.Syscall)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Name) };

        new public static Syscall Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.Syscall,
            Operand.Identifier.Deserialize,
            name => new Syscall(name.Value)
        );
    }

    public record LoadSysfunc(string Name) : Instruction(Opcode.LoadSysfunc)
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Name) };

        new public static LoadSysfunc Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.LoadSysfunc,
            Operand.Identifier.Deserialize,
            name => new LoadSysfunc(name.Value)
        );
    }

    public record AllocateArray(DataType ElementType) : Instruction(Opcode.AllocateArray)
    {
        public override IEnumerable<IOperand> Operands => new IOperand[] { new Operand.DataType(ElementType) };

        new public static AllocateArray Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.AllocateArray,
            DataType.Deserialize,
            dataType => new AllocateArray(dataType)
        );
    }

    public record LoadFunction(string Module, string Function) : Call
    {
        public override IEnumerable<IOperand> Operands => new[] { new Operand.Identifier(Module), new Operand.Identifier(Function) };

        new public static LoadFunction Deserialize(SExpression sexpr) => Deserialize(
            sexpr,
            Opcode.LoadFunction,
            Operand.Identifier.Deserialize,
            Operand.Identifier.Deserialize,
            (moduleName, functionName) => new LoadFunction(moduleName.Value, functionName.Value)
        );
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
        Opcode.LoadData => LoadData.Deserialize(sexpr),
        Opcode.Return => Return.Deserialize(sexpr),
        Opcode.Assert => Assert.Deserialize(sexpr),
        Opcode.Duplicate => Duplicate.Deserialize(sexpr),
        Opcode.Convert => Convert.Deserialize(sexpr),
        Opcode.Allocate => Allocate.Deserialize(sexpr),
        Opcode.Rotate2 => Rotate2.Deserialize(sexpr),
        Opcode.Reinterpret => Reinterpret.Deserialize(sexpr),
        Opcode.Jump => Jump.Deserialize(sexpr),
        Opcode.CJump => CJump.Deserialize(sexpr),
        Opcode.Add => Add.Deserialize(sexpr),
        Opcode.Sub => Sub.Deserialize(sexpr),
        Opcode.Mul => Mul.Deserialize(sexpr),
        Opcode.Div => Div.Deserialize(sexpr),
        Opcode.Mod => Mod.Deserialize(sexpr),
        Opcode.LShift => LShift.Deserialize(sexpr),
        Opcode.Dump => Dump.Deserialize(sexpr),
        Opcode.Compare => Compare.Deserialize(sexpr),
        Opcode.Or => Or.Deserialize(sexpr),
        Opcode.And => And.Deserialize(sexpr),
        Opcode.Xor => Xor.Deserialize(sexpr),
        Opcode.Dec => Dec.Deserialize(sexpr),
        Opcode.Inc => Inc.Deserialize(sexpr),
        Opcode.LoadElement => LoadElement.Deserialize(sexpr),
        Opcode.StoreElement => StoreElement.Deserialize(sexpr),
        Opcode.LoadLength => LoadLength.Deserialize(sexpr),
        Opcode.Call => Call.Deserialize(sexpr),
        Opcode.ZeroExtend => ZeroExtend.Deserialize(sexpr),
        Opcode.Exit => Exit.Deserialize(sexpr),
        Opcode.AllocateArray => AllocateArray.Deserialize(sexpr),
        Opcode.LoadFunction => LoadFunction.Deserialize(sexpr),
        Opcode.LoadSysfunc => LoadSysfunc.Deserialize(sexpr),
        Opcode.Syscall => Syscall.Deserialize(sexpr),
        var opcode => throw new NotImplementedException(opcode.ToString())
    };
}