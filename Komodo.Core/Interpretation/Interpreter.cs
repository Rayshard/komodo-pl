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
    private Dictionary<string, Value.Array> internPool = new Dictionary<string, Value.Array>();

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

                foreach(var instruction in function.Instructions)
                {
                    if(instruction is Instruction.LoadConstant(Operand.Constant.String(var str)) && !internPool.ContainsKey(str))
                    {
                        var bytes = Encoding.UTF8.GetBytes(str);
                        internPool.Add(str, new Value.Array(new DataType.UI8(), memory.AllocateWrite(bytes, true)));
                    }
                }
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
        Call(Program.Entry.Module, Program.Entry.Function, 0);

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
            case Instruction.LoadConstant instr:
                {
                    Value constant = instr.Constant switch
                    {
                        Operand.Constant.I8(var value) => new Value.I8(value),
                        Operand.Constant.UI8(var value) => new Value.UI8(value),
                        Operand.Constant.I16(var value) => new Value.I16(value),
                        Operand.Constant.UI16(var value) => new Value.UI16(value),
                        Operand.Constant.I32(var value) => new Value.I32(value),
                        Operand.Constant.UI32(var value) => new Value.UI32(value),
                        Operand.Constant.I64(var value) => new Value.I64(value),
                        Operand.Constant.UI64(var value) => new Value.UI64(value),
                        Operand.Constant.F32(var value) => new Value.F32(value),
                        Operand.Constant.F64(var value) => new Value.F64(value),
                        Operand.Constant.True => new Value.Bool(true),
                        Operand.Constant.False => new Value.Bool(false),
                        Operand.Constant.Null => new Value.Pointer(Address.NULL, new DataType.Pointer(true)),
                        Operand.Constant.Sizeof(var type) => new Value.UI64(type.ByteSize),
                        Operand.Constant.String(var value) => internPool[value],
                        var value => throw new NotImplementedException(value.ToString())
                    };

                    stack.Push(constant);
                }
                break;
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
            case Instruction.LoadData instr: stack.Push(data[(instr.ModuleName, instr.DataName)]); break;
            case Instruction.LoadSysfunc instr: stack.Push(sysfuncTable[instr.Name]); break;
            case Instruction.Exit instr:
                {
                    exitcode = PopStack<Value.I64>(new DataType.I64()).Value;
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
            case Instruction.AllocateArray instr:
                {
                    var amount = PopStack<Value.UI64>(new DataType.UI64()).Value;
                    var address = memory.AllocateWrite(Enumerable.Range(0, (int)amount).Select(_ => CreateDefault(instr.ElementType)), false, true);
                    stack.Push(new Value.Array(instr.ElementType, address));
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
            case Instruction.StoreElement instr:
                {
                    var array = PopStack<Value.Array>();
                    var arrayLength = memory.ReadUInt64(array.LengthStart);
                    var index = PopStack<Value.UI64>(new DataType.UI64()).Value;
                    var value = PopStack(array.ElementType);

                    if (index >= arrayLength)
                        throw new InterpreterException($"Index {index} is greater than array length: {arrayLength}");

                    memory.Write(array.ElementsStart + index * array.ElementType.ByteSize, value);
                }
                break;
            case Instruction.Add:
            case Instruction.Sub:
            case Instruction.Mul:
            case Instruction.Div:
            case Instruction.Mod:
            case Instruction.Compare:
            case Instruction.LShift:
            case Instruction.Or:
            case Instruction.And:
            case Instruction.Xor:
            case Instruction.LoadElement:
                {
                    var value1 = PopStack();
                    var value2 = PopStack();

                    Value result = (instruction, value1, value2) switch
                    {
                        (Instruction.Add, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 + op2),
                        (Instruction.Mul, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 * op2),
                        (Instruction.Compare(var comparison), _, _) => new Value.Bool(Compare(value1, value2, comparison)),
                        (Instruction.LShift, Value.UI16(var op1), Value.UI8(var op2)) => new Value.UI16(op2 >= 16 ? (UInt16)0 : (UInt16)(op1 << op2)),
                        (Instruction.LShift, Value.I16(var op1), Value.UI8(var op2)) => new Value.I16(op2 >= 16 ? (Int16)0 : (Int16)(op1 << op2)),
                        (Instruction.Or, Value.UI16(var op1), Value.UI16(var op2)) => new Value.UI16((UInt16)(op1 | op2)),
                        (Instruction.Or, Value.I16(var op1), Value.I16(var op2)) => new Value.I16((Int16)(op1 | op2)),
                        (Instruction.Or, Value.Bool(var op1), Value.Bool(var op2)) => new Value.Bool((Byte)(op1 | op2)),
                        (Instruction.And, Value.Bool(var op1), Value.Bool(var op2)) => new Value.Bool((Byte)(op1 & op2)),
                        (Instruction.Xor, Value.Bool(var op1), Value.Bool(var op2)) => new Value.Bool((Byte)(op1 ^ op2)),
                        (Instruction.LoadElement, Value.Array array, Value.UI64(var index)) => index < memory.ReadUInt64(array.LengthStart)
                            ? memory.Read(array.ElementsStart + index * array.ElementType.ByteSize, array.ElementType)
                            : throw new InterpreterException($"Index {index} is greater than array length: {memory.ReadUInt64(array.LengthStart)}"),
                        _ => throw new InterpreterException($"Cannot apply {instruction.Opcode} to {value1.DataType} and {value2.DataType}.")
                    };

                    stack.Push(result);
                }
                break;
            case Instruction.Dec:
            case Instruction.Inc:
            case Instruction.LoadLength:
                {
                    Value result = (instruction, PopStack()) switch
                    {
                        (Instruction.Dec, Value.I64(var value)) => new Value.I64(value - 1),
                        (Instruction.LoadLength, Value.Array array) => new Value.UI64(memory.ReadUInt64(array.LengthStart)),
                        (Instruction(var opcode), var value) => throw new Exception($"Cannot apply {opcode} to {value.DataType}.")
                    };

                    stack.Push(result);
                }
                break;
            case Instruction.Dump instr:
                Config.StandardOutput.WriteLine(PopStack() switch
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
            case Instruction.Call.Direct instr: Call(instr.Module, instr.Function, stackFrame.FramePointer); break;
            case Instruction.Call.Indirect instr: Call(PopStack<Value.Function>(), stackFrame.FramePointer); break;
            case Instruction.Syscall instr: Call(sysfuncTable[instr.Name], stackFrame.FramePointer); break;
            case Instruction.LoadFunction instr: stack.Push(functionTable[(instr.Module, instr.Function)]); break;
            case Instruction.Return instr: PopStackFrame(); break;
            case Instruction.Jump instr:
                {
                    var target = GetFunctionLabelTarget(stackFrame.IP.Module, stackFrame.IP.Function, instr.Label);
                    nextIP = new InstructionPointer(stackFrame.IP.Module, stackFrame.IP.Function, target);
                }
                break;
            case Instruction.CJump instr:
                {
                    if (PopStack<Value.Bool>(new DataType.Bool()).IsTrue)
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
                    var value = PopStack();

                    if (value.DataType is not DataType.Primitive)
                        throw new InterpreterException("Non-primitive types cannot be reinterpreted!");

                    var reinterpretedValue = value.DataType.ByteSize != instr.Target.ByteSize
                        ? throw new Exception($"Cannot interpret {value.DataType} which has size {value.DataType.ByteSize} as {instr.Target} which has size {instr.Target.ByteSize}")
                        : Value.Create(instr.Target, value.AsBytes());

                    stack.Push(reinterpretedValue);
                }
                break;
            case Instruction.ZeroExtend instr:
                {
                    Value result = (PopStack(), instr.Target) switch
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

                    stack.Push(result);
                }
                break;
            default: throw new Exception($"Instruction '{instruction.Opcode.ToString()}' has not been implemented.");
        }
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

    private void Call(string ModuleName, string FunctionName, int framePointer)
    {
        var function = GetFunction(ModuleName, FunctionName);
        var start = new InstructionPointer(ModuleName, FunctionName, 0);

        // Verify that the correct number of args are on the frame's stack
        var expectedNumArgs = function.Parameters.Count;
        var frameStackSize = stack.Count - framePointer;

        if (frameStackSize < expectedNumArgs)
            throw new Exception($"Expected at least {expectedNumArgs} items on the stack, but found {frameStackSize}!");

        // Pop and verify the items on the stack are the correct data type in the order of the parameter types
        var arguments = new (Value, string?)[expectedNumArgs];

        for (int i = 0; i < expectedNumArgs; i++)
        {
            var parameter = function.Parameters[i];
            var actual = stack.Peek().DataType;

            if (actual != parameter.DataType)
                throw new Exception($"Expected {parameter.DataType} for argument {i}, but got {actual}!");

            arguments[i] = (PopStack(parameter.DataType), parameter.Name);
        }

        var locals = function.Locals.Select(l => (l.Key, CreateDefault(l.Value))).ToArray();

        callStack.Push(new StackFrame(start, stack.Count, arguments, locals));
    }

    private void Call(Value.Function target, int framePointer)
    {
        var mangledString = memory.ReadString(target.Address);

        if (sysfuncTable.TryGetValue(mangledString, out var sysfunc))
        {
            // Verify that the correct number of args are on the frame's stack
            var expectedNumArgs = sysfunc.Parameters.Count;
            var frameStackSize = stack.Count - framePointer;

            if (frameStackSize < expectedNumArgs)
                throw new Exception($"Expected at least {expectedNumArgs} items on the stack, but found {frameStackSize}!");

            // Pop and verify the items on the stack are the correct data type in the order of the parameter types
            var arguments = new Value[expectedNumArgs];

            for (int i = 0; i < expectedNumArgs; i++)
            {
                var expected = sysfunc.Parameters[i];
                var actual = stack.Peek().DataType;

                if (actual != expected)
                    throw new Exception($"Expected {expected} for argument {i}, but got {actual}!");

                arguments[i] = PopStack(expected);
            }

            // Push return values onto the stack in reverse order
            sysfunc.Call(arguments).Reverse().ForEach(stack.Push);
        }
        else
        {
            var (moduleName, functionName) = Demangle(mangledString);
            Call(moduleName, functionName, framePointer);
        }
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
        (Value.Pointer ptr1, Value.Pointer ptr2, Comparison.EQ) when ptr1.IsReadonly == ptr2.IsReadonly || ptr1.IsNull || ptr2.IsNull => ptr1.Address == ptr2.Address,
        (Value.Reference ref1, Value.Reference ref2, Comparison.EQ) when ref1.ValueType == ref2.ValueType => ref1.Address == ref2.Address,
        (Value.Pointer ptr, Value.Function func, Comparison.EQ) when ptr.IsReadonly => ptr.Address == func.Address,
        (Value.Pointer ptr, Value.Array array, Comparison.EQ) when ptr.IsReadonly => ptr.Address == array.Address,
        (Value.Array array1, Value.Array array2, Comparison.EQ) => array1.Address == array2.Address,

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
        (Value.Pointer pointer, Value.Reference reference, Comparison.NEQ) when pointer.IsReadonly => pointer.Address != reference.Address,
        (Value.Pointer ptr1, Value.Pointer ptr2, Comparison.NEQ) when ptr1.IsReadonly == ptr2.IsReadonly || ptr1.IsNull || ptr2.IsNull => ptr1.Address != ptr2.Address,
        (Value.Reference ref1, Value.Reference ref2, Comparison.NEQ) when ref1.ValueType == ref2.ValueType => ref1.Address != ref2.Address,
        (Value.Pointer ptr, Value.Function func, Comparison.NEQ) when ptr.IsReadonly => ptr.Address != func.Address,
        (Value.Pointer ptr, Value.Array array, Comparison.NEQ) when ptr.IsReadonly => ptr.Address != array.Address,
        (Value.Array array1, Value.Array array2, Comparison.NEQ) => array1.Address != array2.Address,


        _ => throw new NotImplementedException($"Connot compare {v1.DataType} to {v2.DataType} with {comparison}")
    };

    public string Mangle(string module, string function) => $"{module} {function}";

    public (string Module, string Function) Demangle(string mangledString) => mangledString.Split(' ').Deconstruct() switch
    {
        (var module, (var function, null)) => (module, function),
        var list => throw new Exception($"Invalid mangled string: {mangledString}")
    };
}