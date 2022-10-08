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

    private Stack<Value> stack = new Stack<Value>();
    private Stack<StackFrame> callStack = new Stack<StackFrame>();

    private Memory memory = new Memory();

    public Interpreter(Program program, InterpreterConfig config)
    {
        State = InterpreterState.NotStarted;
        Program = program;
        Config = config;

        foreach (var module in program.Modules)
        {
            foreach (var item in module.Data.Values)
            {
                var array = new Value.Array(new DataType.UI8(), (UInt64)item.Bytes.Length, memory.AllocateWrite(item.Bytes));
                data.Add((module.Name, item.Name), array);
            }

            foreach (var global in module.Globals.Values)
                globals.Add((module.Name, global.Name), Value.CreateDefault(global.DataType));
        }
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
            case Instruction.Syscall instr:
                {
                    switch (instr.Name)
                    {
                        case "GetTime": stack.Push(new Value.UI64((ulong)DateTime.UtcNow.Ticks)); break;
                        case "Write":
                            {
                                var handle = PopStack<Value.I64>().Value;
                                var array = PopStack<Value.Array>(new DataType.Array(new DataType.UI8()));
                                var buffer = memory.Read<Value.UI8>(array.Address, array.ElementType, array.Length)
                                                   .Select(value => value.Value)
                                                   .ToArray();
                                var bufferAsString = Encoding.UTF8.GetString(buffer);

                                switch (handle)
                                {
                                    case 0: Config.StandardOutput.Write(bufferAsString); break;
                                    default: throw new Exception($"Unknown handle: {handle}");
                                }
                            }
                            break;
                        default: throw new Exception($"Unknown Syscall: {instr.Name}");
                    }
                }
                break;
            case Instruction.Exit instr:
                {
                    exitcode = GetValue<Value.I64>(stackFrame, instr.Code, new DataType.I64()).Value;
                    State = InterpreterState.ShuttingDown;
                }
                break;
            case Instruction.Allocate instr:
                {
                    var address = memory.AllocateWrite(Value.CreateDefault(instr.DataType));
                    SetValue(stackFrame, instr.Destination, new Value.Reference(instr.DataType, address));
                }
                break;
            case Instruction.Load instr:
                {
                    var reference = GetValue<Value.Reference>(stackFrame, instr.Reference);
                    var value = memory.Read(reference);

                    SetValue(stackFrame, instr.Destination, value);
                }
                break;
            case Instruction.Store instr:
                {
                    var value = GetValue(stackFrame, instr.Value);
                    var reference = GetValue<Value.Reference>(stackFrame, instr.Reference, new DataType.Reference(value.DataType));

                    memory.Write(reference.Address, value);
                }
                break;
            case Instruction.Move instr: SetValue(stackFrame, instr.Destination, GetValue(stackFrame, instr.Source)); break;
            case Instruction.Binop instr:
                {
                    var source1 = GetValue(stackFrame, instr.Source1);
                    var source2 = GetValue(stackFrame, instr.Source2);

                    Value result = (instr.Opcode, source1, source2) switch
                    {
                        (Opcode.Add, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 + op2),
                        (Opcode.Mul, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 * op2),
                        (Opcode.Eq, Value.I64(var op1), Value.I64(var op2)) => new Value.Bool(op1 == op2),
                        (Opcode.GetElement, Value.UI64(var index), Value.Array a)
                            => index < a.Length
                                ? memory.Read(a.Address + index * a.ElementType.ByteSize, a.ElementType)
                                : throw new Exception($"Index {index} is greater than array length: {a.Length}"),
                        var operands => throw new Exception($"Cannot apply operation to {operands}.")
                    };

                    SetValue(stackFrame, instr.Destination, result);
                }
                break;
            case Instruction.Unop instr:
                {
                    var source = GetValue(stackFrame, instr.Source);

                    Value result = (instr.Opcode, source, instr.DataType) switch
                    {
                        (Opcode.Dec, Value.I64(var op), DataType.I64) => new Value.I64(op - 1),
                        var operands => throw new Exception($"Cannot apply operation to {operands}.")
                    };

                    SetValue(stackFrame, instr.Destination, result);
                }
                break;
            case Instruction.Dump instr:
                Config.StandardOutput.WriteLine(GetValue(stackFrame, instr.Source) switch
                {
                    Value.Array a => a.Length == 0
                        ? "[]"
                        : memory.Read(a.Address, a.ElementType, a.Length).Stringify(", ", ("[", "]")),
                    Value.Type t => t.IsUnknown
                        ? "unknown"
                        : Encoding.UTF8.GetString(memory.Read(t.Address + 8, memory.ReadUInt64(t.Address))),
                    Value.Reference r => r.IsSet
                        ? $"ref {memory.Read(r.Address, r.ValueType)}"
                        : $"ref ({r.DataType} null)",
                    var value => value
                }); break;
            case Instruction.Assert instr:
                {
                    var value1 = GetValue(stackFrame, instr.Source1);
                    var value2 = GetValue(stackFrame, instr.Source2);

                    if (value1 != value2)
                        throw new InterpreterException($"Assertion Failed: {value1} != {value2}.");
                }
                break;
            case Instruction.Call instr:
                {
                    var receivedArgs = instr.Args.Select(source => GetValue(stackFrame, source)).ToArray();
                    PushStackFrame((instr.Module, instr.Function), receivedArgs);
                }
                break;
            case Instruction.Return instr:
                {
                    var values = instr.Sources.Select(source => GetValue(stackFrame, source)).ToArray();
                    PopStackFrame(values);
                }
                break;
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
            case Instruction.Convert instr: SetValue(stackFrame, instr.Destination, GetValue(stackFrame, instr.Value).ConvertTo(instr.Target)); break;
            case Instruction.Reinterpret instr: SetValue(stackFrame, instr.Destination, GetValue(stackFrame, instr.Value).ReinterpretAs(instr.Target)); break;
            default: throw new Exception($"Instruction '{instruction.Opcode.ToString()}' has not been implemented.");
        }
    }

    private Value GetValue(StackFrame stackFrame, Operand.Source source, DataType? expectedDataType = null)
    {
        var value = source switch
        {
            Operand.Constant(var v) => v,
            Operand.Local.Indexed(var i) => stackFrame.Locals[(int)i],
            Operand.Local.Named(var n) => stackFrame.GetLocal(n),
            Operand.Global(var module, var name) => globals[(module, name)],
            Operand.Arg.Indexed(var i) => stackFrame.Arguments[(int)i],
            Operand.Arg.Named(var n) => stackFrame.GetArgument(n),
            Operand.Stack => PopStack(),
            Operand.Null(var valueType) => new Value.Reference(valueType, Address.NULL),
            Operand.Data(var module, var name) => data[(module, name)],
            Operand.Array(var elementType, var elements) => new Value.Array(
                elementType,
                (UInt64)elements.Count,
                elements.Count == 0
                    ? Address.NULL
                    : memory.AllocateWrite(elements.Select(e => GetValue(stackFrame, e, elementType)))
            ),
            Operand.Typeof operand => new Value.Type(memory.AllocateWrite(operand.Type.AsMangledString(), true)),
            _ => throw new Exception($"Invalid source: {source}")
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

        if(stackTop is T converted)
            return converted;

        stack.Push(stackTop);
        throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");
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

        var locals = function.Locals.Select(l => (Value.CreateDefault(l.DataType), l.Name)).ToArray();

        callStack.Push(new StackFrame(start, stack.Count, arguments, locals));
    }

    private void PopStackFrame(Value[] returnValues)
    {
        if (callStack.TryPop(out var frame))
        {
            var function = GetFunction(frame.IP.Module, frame.IP.Function);

            // Verify return values
            if (returnValues.Length != function.Returns.Count)
                throw new Exception($"Expected {function.Returns.Count()} return values but got {returnValues.Length}.");

            foreach (var (actual, expected, i) in returnValues.Select((value, i) => (value.DataType, function.Returns[i], i)))
            {
                if (actual != expected)
                    throw new Exception($"Expeceted {expected} for function return {i}, but got {actual}!");
            }

            // Erase current stack from stack
            while (stack.Count > frame.FramePointer)
                stack.Pop();

            // Push return values onto the stack in reverse order
            foreach (var value in returnValues.Reverse())
                stack.Push(value);
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());
    public InstructionPointer[] GetStackTrace() => callStack.Select(sf => sf.LastIP ?? sf.IP).ToArray();

    public Function GetFunction(string module, string function) => Program.GetModule(module).GetFunction(function);
    private Instruction GetInstruction(InstructionPointer ip) => Program.GetModule(ip.Module).GetFunction(ip.Function).Instructions[(int)ip.Index];
    public UInt64 GetFunctionLabelTarget(string module, string function, string label) => Program.GetModule(module).GetFunction(function).Labels[label].Target;
}