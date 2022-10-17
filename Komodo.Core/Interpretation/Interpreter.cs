using System.Text;
using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public enum InterpreterState { NotStarted, Running, ShuttingDown, Terminated }

public record InterpreterConfig(TextWriter StandardOutput);

public class InterpreterException : Exception
{
    public InterpreterException(string message) : base(message) { }
    public InterpreterException(string message, Exception inner) : base(message, inner) { }
}

public class Interpreter
{
    public InterpreterState State { get; private set; }
    public Program Program { get; }
    public InterpreterConfig Config { get; }

    private Dictionary<(string Module, string Name), Value> globals = new Dictionary<(string Module, string Name), Value>();
    private Dictionary<(string Module, string Name), Value.Array> data = new Dictionary<(string Module, string Name), Value.Array>();
    private Dictionary<(string Module, string Name), Value.Function> functionTable = new Dictionary<(string Module, string Name), Value.Function>();
    private Dictionary<string, Sysfunc> sysfuncTable = new Dictionary<string, Sysfunc>();

    private Stack<Value> stack = new Stack<Value>();
    private Stack<StackFrame> callStack = new Stack<StackFrame>();

    private Memory memory = new Memory();

    public Interpreter(Program program, InterpreterConfig config)
    {
        State = InterpreterState.NotStarted;
        Program = program;
        Config = config;

        InitializeSysfuncTable();

        foreach (var module in program.Modules.Values)
        {
            foreach (var item in module.Data.Values)
            {
                var array = new Value.Array(new DataType.UI8(), memory.AllocateWrite(item.Bytes, true));
                data.Add((module.Name, item.Name), array);
            }

            foreach (var global in module.Globals.Values)
                globals.Add((module.Name, global.Name), CreateDefault(global.DataType));

            foreach (var function in module.Functions.Values)
            {
                var parameters = function.Parameters.Select(p => p.DataType).ToVSROCollection();
                var address = memory.AllocateWrite(Mangle(module.Name, function.Name), true);

                functionTable.Add((module.Name, function.Name), new Value.Function(parameters, function.Returns, address));
            }
        }
    }

    private void InitializeSysfuncTable()
    {
        sysfuncTable.Add("Write", new Sysfunc.Write(memory.AllocateWrite("Write", true), (handle, data) =>
        {
            var buffer = memory.Read<Value.UI8>(data.ElementsStart, data.ElementType, memory.ReadUInt64(data.LengthStart))
                               .Select(value => value.Value)
                               .ToArray();
            var bufferAsString = Encoding.UTF8.GetString(buffer);

            switch (handle.Value)
            {
                case 0: Config.StandardOutput.Write(bufferAsString); break;
                default: throw new Exception($"Unknown handle: {handle}");
            }
        }));

        sysfuncTable.Add("GetTime", new Sysfunc.GetTime(memory.AllocateWrite("GetTime", true), () => (UInt64)DateTime.UtcNow.Ticks));
    }

    public Int64 Run()
    {
        Int64 exitcode = 0;
        int numInstructionsExecuted = 0;

        State = InterpreterState.Running;

        // Call Program Entry
        PushStackFrame(Program.Entry, new Value[0]);

        while (State == InterpreterState.Running)
        {
            if (callStack.Count == 0)
                throw new Exception("Exited with an empty call stack");

            var stackFrame = callStack.Peek();

            try
            {
                ExecuteNextInstruction(stackFrame, ref exitcode, out var nextIP);

                numInstructionsExecuted++;

                var stackAsString = StackToString();
                stackAsString = stackAsString.Length == 0 ? " Empty" : ("\n" + stackAsString);

                Logger.Debug($"Current Stack:{stackAsString}");

                stackFrame.IP = nextIP;
            }
            catch (InterpreterException e)
            {
                Config.StandardOutput.WriteLine($"Exception: {e.Message}");

                foreach (var ip in GetStackTrace())
                    Config.StandardOutput.WriteLine($"    at {ip}            {GetInstruction(ip).AsSExpression()}");

                exitcode = 1;
                break;
            }
            catch (Exception e)
            {
                Logger.Error($"An error occurred at {stackFrame.IP}: {e.Message}" + (e.StackTrace is null ? "" : $"\n{e.StackTrace}"));

                exitcode = 1;
                break;
            }
        }

        State = InterpreterState.Terminated;

        Logger.Debug($"Number of instructions executed: {numInstructionsExecuted}");
        return exitcode;
    }

    private void ExecuteNextInstruction(StackFrame stackFrame, ref Int64 exitcode, out InstructionPointer nextIP)
    {
        var instruction = GetInstruction(stackFrame.IP);

        stackFrame.LastIP = stackFrame.IP;
        nextIP = stackFrame.IP + 1;

        Logger.Debug($"{stackFrame.IP}: {instruction}");

        switch (instruction)
        {
            case Instruction.LoadConstant instr: stack.Push(Value.FromConstant(instr.Constant)); break;
            case Instruction.LoadGlobal instr: stack.Push(globals[(instr.ModuleName, instr.GlobalName)]); break;
            case Instruction.StoreGlobal instr:
                {
                    var expectedDataType = globals[(instr.ModuleName, instr.GlobalName)].DataType;
                    var value = PopStack(expectedDataType);

                    globals[(instr.ModuleName, instr.GlobalName)] = value;
                }
                break;
            case Instruction.LoadLocal instr: stack.Push(stackFrame.GetLocal(instr.Name)); break;
            case Instruction.StoreLocal instr:
                {
                    var expectedDataType = stackFrame.GetLocal(instr.Name).DataType;
                    var value = PopStack(expectedDataType);

                    stackFrame.SetLocal(instr.Name, value);
                }
                break;
            case Instruction.LoadArg instr: stack.Push(stackFrame.GetArgument(instr.Name)); break;
            case Instruction.Exit instr:
                {
                    exitcode = GetValue<Value.I64>(stackFrame, instr.Code, new DataType.I64()).Value;
                    State = InterpreterState.ShuttingDown;
                }
                break;
            case Instruction.Duplicate instr: stack.Push(PeekStack()); break;
            case Instruction.Rotate2 instr:
                {
                    var first = PopStack();
                    var second = PopStack();
                    stack.Push(first);
                    stack.Push(second);
                }
                break;
            case Instruction.Allocate instr:
                {
                    if (instr.DataType is null) // Allocate from size
                    {
                        var amount = PopStack<Value.UI64>(new DataType.UI64()).Value;
                        var address = memory.Allocate(amount);
                        stack.Push(new Value.Pointer(address, new DataType.Pointer(false)));
                    }
                    else // Allocate from data type
                    {
                        var address = memory.AllocateWrite(CreateDefault(instr.DataType));
                        stack.Push(new Value.Reference(instr.DataType, address));
                    }
                }
                break;
            case Instruction.LoadMem instr:
                {
                    Value value;

                    if (instr.DataType is null)
                    {
                        var reference = PopStack<Value.Reference>();
                        value = memory.Read(reference);
                    }
                    else
                    {
                        var pointer = PopStack<Value.Pointer>();
                        value = memory.Read(pointer, instr.DataType);
                    }

                    stack.Push(value);
                }
                break;
            case Instruction.StoreMem instr:
                {
                    var destination = PopStack();
                    var value = PopStack();

                    var address = destination switch
                    {
                        Value.Reference reference when reference.ValueType == value.DataType => reference.Address,
                        Value.Pointer pointer when pointer.IsReadWrite => pointer.Address,
                        _ => throw new InterpreterException($"Invalid destination. Expected a RWPtr or {new DataType.Reference(value.DataType)}, but found {destination.DataType}")
                    };

                    memory.Write(address, value);
                }
                break;
            case Instruction.Binop instr:
                {
                    var source1 = GetValue(stackFrame, instr.Source1);
                    var source2 = GetValue(stackFrame, instr.Source2);

                    Value result = (instr.Opcode, source1, source2) switch
                    {
                        (Opcode.Add, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 + op2),
                        (Opcode.Mul, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 * op2),
                        (Opcode.Eq, Value.I64(var op1), Value.I64(var op2)) => new Value.Bool(op1 == op2),
                        (Opcode.LShift, Value.UI16(var op1), Value.UI8(var op2)) => new Value.UI16(op2 >= 16 ? (UInt16)0 : (UInt16)(op1 << op2)),
                        (Opcode.LShift, Value.I16(var op1), Value.UI8(var op2)) => new Value.I16(op2 >= 16 ? (Int16)0 : (Int16)(op1 << op2)),
                        (Opcode.BOR, Value.UI16(var op1), Value.UI16(var op2)) => new Value.UI16((UInt16)(op1 | op2)),
                        (Opcode.BOR, Value.I16(var op1), Value.I16(var op2)) => new Value.I16((Int16)(op1 | op2)),
                        (Opcode.GetElement, Value.UI64(var index), Value.Array a) => index < memory.ReadUInt64(a.LengthStart)
                            ? memory.Read(a.ElementsStart + index * a.ElementType.ByteSize, a.ElementType)
                            : throw new InterpreterException($"Index {index} is greater than array length: {memory.ReadUInt64(a.LengthStart)}"),
                        var operands => throw new InterpreterException($"Cannot apply operation to {operands}.")
                    };

                    SetValue(stackFrame, instr.Destination, result);
                }
                break;
            case Instruction.Unop instr:
                {
                    var source = GetValue(stackFrame, instr.Source);

                    Value result = (instr.Opcode, source) switch
                    {
                        (Opcode.Dec, Value.I64(var op)) => new Value.I64(op - 1),
                        (Opcode.GetLength, Value.Array array) => new Value.UI64(memory.ReadUInt64(array.LengthStart)),
                        var operands => throw new Exception($"Cannot apply {instr.Opcode} to {source.DataType}.")
                    };

                    SetValue(stackFrame, instr.Destination, result);
                }
                break;
            case Instruction.Dump instr:
                Config.StandardOutput.WriteLine(GetValue(stackFrame, instr.Source) switch
                {
                    Value.Array value => value.Allocated
                        ? memory.Read(value.ElementsStart, value.ElementType, memory.ReadUInt64(value.LengthStart)).Stringify(", ", ("[", "]"))
                        : throw new Exception("Array is not allocated"), // This should never be the case because the default value for an array is an allocated array of size 0
                    Value.Reference value => value.Allocated
                        ? $"ref {memory.Read(value.Address, value.ValueType)}"
                        : $"ref ({value.DataType} null)",
                    Value.Function value => value.Allocated
                        ? $"function {Demangle(memory.ReadString(value.Address))}"
                        : $"unknown ({value.Parameters.Stringify(", ")}) -> ({value.Returns.Stringify(", ")})",
                    var value => value
                }); break;
            case Instruction.Assert instr:
                {
                    var value1 = PopStack();
                    var value2 = PopStack();

                    if (!Compare(value1, value2, instr.Comparison))
                        throw new InterpreterException($"Assertion Failed: {value1} {instr.Comparison} {value2}");
                }
                break;
            case Instruction.Call.Direct instr:
                {
                    var receivedArgs = instr.Args.Select(arg => GetValue(stackFrame, arg)).ToArray();
                    PushStackFrame((instr.Module, instr.Function), receivedArgs);
                }
                break;
            case Instruction.Call.Indirect instr:
                {
                    var source = GetValue<Value.Function>(stackFrame, instr.Source);
                    var receivedArgs = instr.Args.Select(arg => GetValue(stackFrame, arg)).ToArray();

                    Call(source, receivedArgs);
                }
                break;
            case Instruction.Return instr: PopStackFrame(); break;
            case Instruction.Jump instr:
                {
                    var condition = instr.Condition is null || GetValue<Value.Bool>(stackFrame, instr.Condition, new DataType.Bool()).IsTrue;

                    if (condition)
                    {
                        var target = GetFunctionLabelTarget(stackFrame.IP.Module, stackFrame.IP.Function, instr.Label);
                        nextIP = new InstructionPointer(stackFrame.IP.Module, stackFrame.IP.Function, target);
                    }
                }
                break;
            case Instruction.Convert instr:
                {
                    Value result = (PopStack(), instr.Target) switch
                    {
                        (Value.I8(var value), DataType.I8) => new Value.I8(value),
                        (Value.I8(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.I8(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.I8(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.I8(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.I8(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.I8(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.I8(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.I8(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.I8(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.I8(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.UI8(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.UI8(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.UI8(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.UI8(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.UI8(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.UI8(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.UI8(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.UI8(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.UI8(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.UI8(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.UI8(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.I16(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.I16(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.I16(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.I16(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.I16(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.I16(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.I16(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.I16(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.I16(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.I16(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.I16(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.UI16(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.UI16(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.UI16(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.UI16(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.UI16(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.UI16(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.UI16(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.UI16(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.UI16(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.UI16(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.UI16(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.I32(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.I32(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.I32(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.I32(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.I32(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.I32(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.I32(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.I32(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.I32(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.I32(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.I32(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.UI32(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.UI32(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.UI32(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.UI32(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.UI32(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.UI32(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.UI32(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.UI32(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.UI32(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.UI32(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.UI32(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.I64(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.I64(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.I64(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.I64(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.I64(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.I64(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.I64(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.I64(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.I64(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.I64(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.I64(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.UI64(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.UI64(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.UI64(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.UI64(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.UI64(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.UI64(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.UI64(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.UI64(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.UI64(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.UI64(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.UI64(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.F32(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.F32(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.F32(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.F32(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.F32(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.F32(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.F32(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.F32(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.F32(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.F32(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.F32(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.F64(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.F64(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.F64(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.F64(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.F64(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.F64(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.F64(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.F64(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.F64(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.F64(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.F64(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.Bool(var value), DataType.I8) => new Value.I8((SByte)value),
                        (Value.Bool(var value), DataType.UI8) => new Value.UI8((Byte)value),
                        (Value.Bool(var value), DataType.I16) => new Value.I16((Int16)value),
                        (Value.Bool(var value), DataType.UI16) => new Value.UI16((UInt16)value),
                        (Value.Bool(var value), DataType.I32) => new Value.I32((Int32)value),
                        (Value.Bool(var value), DataType.UI32) => new Value.UI32((UInt32)value),
                        (Value.Bool(var value), DataType.I64) => new Value.I64((Int64)value),
                        (Value.Bool(var value), DataType.UI64) => new Value.UI64((UInt64)value),
                        (Value.Bool(var value), DataType.F32) => new Value.F32((Single)value),
                        (Value.Bool(var value), DataType.F64) => new Value.F64((Double)value),
                        (Value.Bool(var value), DataType.Bool) => new Value.Bool(value != 0),

                        (Value.Pointer value, DataType.Pointer target) when (value.IsReadonly && value.IsReadonly) || value.IsReadWrite
                            => new Value.Pointer(value.Address, target),

                        (Value.Reference value, DataType.Pointer target) when target.IsReadonly
                            => new Value.Pointer(value.Address, target),

                        (Value.Array value, DataType.Pointer target) when target.IsReadonly
                            => new Value.Pointer(value.ElementsStart, target),

                        (Value.Function value, DataType.Pointer target) when target.IsReadonly
                            => new Value.Pointer(value.Address, target),

                        (var value, var target) => throw new InterpreterException($"Cannot apply operation to {value.DataType} and {target}.")
                    };

                    stack.Push(result);
                }
                break;
            case Instruction.Reinterpret instr:
                {
                    var value = GetValue(stackFrame, instr.Source);

                    if (value.DataType is not DataType.Primitive)
                        throw new InterpreterException("Non-primitive types cannot be reinterpreted!");

                    var reinterpretedValue = value.DataType.ByteSize != instr.Target.ByteSize
                        ? throw new Exception($"Cannot interpret {value.DataType} which has size {value.DataType.ByteSize} as {instr.Target} which has size {instr.Target.ByteSize}")
                        : Value.Create(instr.Target, value.AsBytes());

                    SetValue(stackFrame, instr.Destination, reinterpretedValue);
                }
                break;
            case Instruction.ZeroExtend instr:
                {
                    Value result = (GetValue(stackFrame, instr.Source), instr.Target) switch
                    {
                        (Value.I8(var value), DataType.I16) => new Value.I16((Int16)((Int16)value & 0xFF)),
                        (Value.I8(var value), DataType.UI16) => new Value.UI16((UInt16)((UInt16)value & 0xFF)),
                        (Value.I8(var value), DataType.I32) => new Value.I32((Int32)((Int32)value & 0xFF)),
                        (Value.I8(var value), DataType.UI32) => new Value.UI32((UInt32)((UInt32)value & 0xFF)),
                        (Value.I8(var value), DataType.I64) => new Value.I64((Int64)((Int64)value & 0xFF)),
                        (Value.I8(var value), DataType.UI64) => new Value.UI64((UInt64)((UInt64)value & 0xFF)),

                        (Value.UI8(var value), DataType.I16) => new Value.I16((Int16)((Int16)value & 0xFF)),
                        (Value.UI8(var value), DataType.UI16) => new Value.UI16((UInt16)((UInt16)value & 0xFF)),
                        (Value.UI8(var value), DataType.I32) => new Value.I32((Int32)((Int32)value & 0xFF)),
                        (Value.UI8(var value), DataType.UI32) => new Value.UI32((UInt32)((UInt32)value & 0xFF)),
                        (Value.UI8(var value), DataType.I64) => new Value.I64((Int64)((Int64)value & 0xFF)),
                        (Value.UI8(var value), DataType.UI64) => new Value.UI64((UInt64)((UInt64)value & 0xFF)),

                        (Value.I16(var value), DataType.I32) => new Value.I32((Int32)((Int32)value & 0xFFFF)),
                        (Value.I16(var value), DataType.UI32) => new Value.UI32((UInt32)((UInt32)value & 0xFFFF)),
                        (Value.I16(var value), DataType.I64) => new Value.I64((Int64)((Int64)value & 0xFFFF)),
                        (Value.I16(var value), DataType.UI64) => new Value.UI64((UInt64)((UInt64)value & 0xFFFF)),

                        (Value.UI16(var value), DataType.I32) => new Value.I32((Int32)((Int32)value & 0xFFFF)),
                        (Value.UI16(var value), DataType.UI32) => new Value.UI32((UInt32)((UInt32)value & 0xFFFF)),
                        (Value.UI16(var value), DataType.I64) => new Value.I64((Int64)((Int64)value & 0xFFFF)),
                        (Value.UI16(var value), DataType.UI64) => new Value.UI64((UInt64)((UInt64)value & 0xFFFF)),

                        (Value.I32(var value), DataType.I64) => new Value.I64((Int64)((Int64)value & 0xFFFFFFFF)),
                        (Value.I32(var value), DataType.UI64) => new Value.UI64((UInt64)((UInt64)value & 0xFFFFFFFF)),

                        (Value.UI32(var value), DataType.I64) => new Value.I64((Int64)((Int64)value & 0xFFFFFFFF)),
                        (Value.UI32(var value), DataType.UI64) => new Value.UI64((UInt64)((UInt64)value & 0xFFFFFFFF)),

                        var operands => throw new InterpreterException($"Cannot apply operation to {operands}.")
                    };

                    SetValue(stackFrame, instr.Destination, result);
                }
                break;
            default: throw new Exception($"Instruction '{instruction.Opcode.ToString()}' has not been implemented.");
        }
    }

    private Value GetValue(StackFrame stackFrame, Operand.Source source, DataType? expectedDataType = null)
    {
        var value = source switch
        {
            Operand.Constant.I8(var constant) => new Value.I8(constant),
            Operand.Constant.UI8(var constant) => new Value.UI8(constant),
            Operand.Constant.I16(var constant) => new Value.I16(constant),
            Operand.Constant.UI16(var constant) => new Value.UI16(constant),
            Operand.Constant.I32(var constant) => new Value.I32(constant),
            Operand.Constant.UI32(var constant) => new Value.UI32(constant),
            Operand.Constant.I64(var constant) => new Value.I64(constant),
            Operand.Constant.UI64(var constant) => new Value.UI64(constant),
            Operand.Constant.F32(var constant) => new Value.F32(constant),
            Operand.Constant.F64(var constant) => new Value.F64(constant),
            Operand.Constant.True => new Value.Bool(true),
            Operand.Constant.False => new Value.Bool(false),
            Operand.Local.Indexed(var i) => stackFrame.Locals[(int)i],
            Operand.Local.Named(var n) => stackFrame.GetLocal(n),
            Operand.Global(var module, var name) => globals[(module, name)],
            Operand.Arg.Indexed(var i) => stackFrame.Arguments[(int)i],
            Operand.Arg.Named(var n) => stackFrame.GetArgument(n),
            Operand.Pop p => PopStack(p.Expected),
            Operand.Data(var module, var name) => data[(module, name)],
            Operand.Array(var elementType, var elements) => new Value.Array(
                elementType,
                memory.AllocateWrite(elements.Select(e => GetValue(stackFrame, e, elementType)), false, true)
            ),
            Operand.Function operand => functionTable[(operand.ModuleName, operand.FunctionName)],
            Operand.Sysfunc operand => sysfuncTable[operand.Name],
            _ => throw new Exception($"Invalid source: {source}"),
        };

        if (expectedDataType is not null && value.DataType != expectedDataType)
            throw new InterpreterException($"Invalid data type for source: {source}. Expected {expectedDataType}, but found {value.DataType}");

        return value;
    }

    private T GetValue<T>(StackFrame stackFrame, Operand.Source source, DataType? expectedDataType = null) where T : Value
        => GetValue(stackFrame, source).As<T>();

    private void SetValue(StackFrame stackFrame, Operand.Destination destination, Value value)
    {
        Action setter;
        DataType destDataType;

        switch (destination)
        {
            case Operand.Local.Indexed l:
                {
                    setter = delegate { stackFrame.Locals[l.Index] = value; };
                    destDataType = stackFrame.Locals[l.Index].DataType;
                }
                break;
            case Operand.Local.Named l:
                {
                    setter = delegate { stackFrame.SetLocal(l.Name, value); };
                    destDataType = stackFrame.GetLocal(l.Name).DataType;
                }
                break;
            case Operand.Global g:
                {
                    setter = delegate { globals[(g.Module, g.Name)] = value; };
                    destDataType = globals[(g.Module, g.Name)].DataType;
                }
                break;
            case Operand.Stack:
                {
                    setter = delegate { stack.Push(value); };
                    destDataType = value.DataType;
                }
                break;
            default: throw new Exception($"Invalid destination: {destination}");
        }

        if (destDataType != value.DataType)
            throw new Exception($"Invalid data type for destination: {destination}. Expected {value.DataType}, but found {destDataType}");

        setter();
    }

    private Value PopStack(DataType? dt = null)
    {
        if (!stack.TryPeek(out var stackTop))
            throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

        if (dt is not null && stackTop.DataType != dt)
            throw new InvalidCastException($"Unable to pop '{dt}' off stack. Found '{stackTop.DataType}'");

        return stack.Pop();
    }

    private T PopStack<T>(DataType? dt = null) where T : Value
    {
        var stackTop = PopStack(dt);

        if (stackTop is T converted)
            return converted;

        stack.Push(stackTop);
        throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");
    }

    private Value PeekStack(DataType? dt = null)
    {
        if (!stack.TryPeek(out var stackTop))
            throw new InvalidOperationException("Cannot peek value from stack. The stack is empty.");

        if (dt is not null && stackTop.DataType != dt)
            throw new InvalidCastException($"Peeked '{dt}' from top of stack but expected '{stackTop.DataType}'");

        return stackTop;
    }

    private T PeekStack<T>(DataType? dt = null) where T : Value
    {
        var stackTop = PeekStack(dt);

        if (stackTop is T converted)
            return converted;

        throw new InvalidCastException($"Peeked '{typeof(T)}' from top of stack, but expected '{stackTop.GetType()}'");
    }

    private void PushStackFrame((string Module, string Function) Target, Value[] args)
    {
        var function = GetFunction(Target.Module, Target.Function);
        var start = new InstructionPointer(Target.Module, Target.Function, 0);

        // Verify and create arguments
        if (args.Length != function.Parameters.Count)
            throw new Exception($"Target function required {function.Parameters.Count} arguments, but only {args.Length} were given!");

        var arguments = new (Value, string?)[args.Length];

        foreach (var (arg, param, index) in args.Select((a, i) => (a, function.Parameters[i], i)))
        {
            if (arg.DataType != param.DataType)
                throw new Exception($"Expeceted {param.DataType} for target function's parameter {index}, but got {arg.DataType}!");

            arguments[index] = (arg, param.Name);
        }

        var locals = function.Locals.Select(l => (CreateDefault(l.DataType), l.Name)).ToArray();

        callStack.Push(new StackFrame(start, stack.Count, arguments, locals));
    }

    private void Call(Value.Function target, Value[] args)
    {
        var mangledString = memory.ReadString(target.Address);

        if (sysfuncTable.TryGetValue(mangledString, out var sysfunc))
        {
            // Verify arguments
            if (args.Length != sysfunc.Parameters.Count)
                throw new Exception($"Sysfunc {mangledString} requires {sysfunc.Parameters.Count} arguments, but only {args.Length} were given!");

            var arguments = new (Value, string?)[args.Length];

            foreach (var (arg, param, index) in args.Select((a, i) => (a, sysfunc.Parameters[i], i)))
            {
                if (arg.DataType != param)
                    throw new Exception($"Expeceted {param} for target function's parameter {index}, but got {arg.DataType}!");
            }

            // Push return values onto the stack in reverse order
            sysfunc.Call(args).Reverse().ForEach(stack.Push);
        }
        else { PushStackFrame(Demangle(mangledString), args); }
    }

    private void PopStackFrame()
    {
        if (callStack.TryPop(out var frame))
        {
            var function = GetFunction(frame.IP.Module, frame.IP.Function);

            // Verify that the correct number of returns are on the frame's stack
            var expectedNumReturns = function.Returns.Count;
            var frameStackSize = stack.Count - frame.FramePointer;

            if (frameStackSize != expectedNumReturns)
                throw new Exception($"Expected {expectedNumReturns} items on the stack, but found {frameStackSize}!");

            // Verify the items on the stack are the correct data type in the order of the return types
            for (int i = 0; i < expectedNumReturns; i++)
            {
                var expected = function.Returns[i];
                var actual = stack.ElementAt(i).DataType;

                if (actual != expected)
                    throw new Exception($"Expected {expected} for return {i}, but got {actual}!");
            }
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());
    public InstructionPointer[] GetStackTrace() => callStack.Select(sf => sf.LastIP ?? sf.IP).ToArray();

    public Function GetFunction(string moduleName, string functionName) => Program.Modules[moduleName].Functions[functionName];
    private Instruction GetInstruction(InstructionPointer ip) => GetFunction(ip.Module, ip.Function).Instructions[(int)ip.Index];
    public UInt64 GetFunctionLabelTarget(string moduleName, string functionName, string label) => GetFunction(moduleName, functionName).Labels[label].Target;

    public Value CreateDefault(DataType dataType) => dataType switch
    {
        DataType.I8 => new Value.I8(0),
        DataType.UI8 => new Value.UI8(0),
        DataType.I64 => new Value.I64(0),
        DataType.UI64 => new Value.UI64(0),
        DataType.Bool => new Value.Bool(false),
        DataType.Array(var elementType) => new Value.Array(
            elementType,
            memory.AllocateWrite(new Value[0], prefixWithNumValues: true)
        ),
        DataType.Reference(var valueType) => new Value.Reference(valueType, Address.NULL),
        DataType.Function(var parameters, var returns) => new Value.Function(parameters, returns, Address.NULL),
        DataType.Pointer type => new Value.Pointer(Address.NULL, type),
        _ => throw new NotImplementedException(dataType.ToString())
    };

    public bool Compare(Value v1, Value v2, Comparison comparison) => (v1, v2, comparison) switch
    {
        (Value.I8(var value1), Value.I8(var value2), Comparison.EQ) => value1 == value2,
        (Value.UI8(var value1), Value.UI8(var value2), Comparison.EQ) => value1 == value2,
        (Value.I16(var value1), Value.I16(var value2), Comparison.EQ) => value1 == value2,
        (Value.UI16(var value1), Value.UI16(var value2), Comparison.EQ) => value1 == value2,
        (Value.I32(var value1), Value.I32(var value2), Comparison.EQ) => value1 == value2,
        (Value.UI32(var value1), Value.UI32(var value2), Comparison.EQ) => value1 == value2,
        (Value.I64(var value1), Value.I64(var value2), Comparison.EQ) => value1 == value2,
        (Value.UI64(var value1), Value.UI64(var value2), Comparison.EQ) => value1 == value2,
        (Value.F32(var value1), Value.F32(var value2), Comparison.EQ) => value1 == value2,
        (Value.F64(var value1), Value.F64(var value2), Comparison.EQ) => value1 == value2,
        (Value.Bool(var value1), Value.Bool(var value2), Comparison.EQ) => value1 == value2,
        (Value.Pointer pointer, Value.Reference reference, Comparison.EQ) => pointer.Address == reference.Address,
        (Value.Pointer ptr1, Value.Pointer ptr2, Comparison.EQ) => ptr1.Address == ptr2.Address,
        (Value.Reference ref1, Value.Reference ref2, Comparison.EQ) when ref1.ValueType == ref2.ValueType => ref1.Address == ref2.Address,


        (Value.I8(var value1), Value.I8(var value2), Comparison.NEQ) => value1 != value2,
        (Value.UI8(var value1), Value.UI8(var value2), Comparison.NEQ) => value1 != value2,
        (Value.I16(var value1), Value.I16(var value2), Comparison.NEQ) => value1 != value2,
        (Value.UI16(var value1), Value.UI16(var value2), Comparison.NEQ) => value1 != value2,
        (Value.I32(var value1), Value.I32(var value2), Comparison.NEQ) => value1 != value2,
        (Value.UI32(var value1), Value.UI32(var value2), Comparison.NEQ) => value1 != value2,
        (Value.I64(var value1), Value.I64(var value2), Comparison.NEQ) => value1 != value2,
        (Value.UI64(var value1), Value.UI64(var value2), Comparison.NEQ) => value1 != value2,
        (Value.F32(var value1), Value.F32(var value2), Comparison.NEQ) => value1 != value2,
        (Value.F64(var value1), Value.F64(var value2), Comparison.NEQ) => value1 != value2,
        (Value.Bool(var value1), Value.Bool(var value2), Comparison.NEQ) => value1 != value2,
        (Value.Pointer pointer, Value.Reference reference, Comparison.NEQ) => pointer.Address != reference.Address,
        (Value.Pointer ptr1, Value.Pointer ptr2, Comparison.NEQ) => ptr1.Address != ptr2.Address,
        (Value.Reference ref1, Value.Reference ref2, Comparison.NEQ) when ref1.ValueType == ref2.ValueType => ref1.Address != ref2.Address,

        _ => throw new NotImplementedException($"Connot compare {v1.DataType} to {v2.DataType} with {comparison}")
    };

    public string Mangle(string module, string function) => $"{module} {function}";

    public (string Module, string Function) Demangle(string mangledString) => mangledString.Split(' ').Deconstruct() switch
    {
        (var module, (var function, null)) => (module, function),
        var list => throw new Exception($"Invalid mangled string: {mangledString}")
    };
}