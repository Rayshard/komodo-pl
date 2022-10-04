using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public enum InterpreterState { NotStarted, Running, ShuttingDown, Terminated }

public record InterpreterConfig(TextWriter StandardOutput);

public class Interpreter
{
    public InterpreterState State { get; private set; }
    public Program Program { get; }
    public InterpreterConfig Config { get; }

    private Dictionary<string, UInt64> dataItems = new Dictionary<string, UInt64>();
    private Dictionary<(string Module, string Name), Value> globals = new Dictionary<(string Module, string Name), Value>();

    private Stack<Value> stack = new Stack<Value>();
    private Stack<StackFrame> callStack = new Stack<StackFrame>();

    private Heap heap = new Heap();

    public Interpreter(Program program, InterpreterConfig config)
    {
        State = InterpreterState.NotStarted;
        Program = program;
        Config = config;

        foreach(var dataItem in program.DataSegemnt.Values)
            dataItems.Add(dataItem.Name, heap.Allocate(dataItem.Bytes));

        foreach (var module in program.Modules)
        {
            foreach (var global in module.Globals.Values)
                globals.Add((module.Name, global.Name), global.DefaultValue is null ? Value.CreateDefault(global.DataType) : global.DefaultValue);
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
            catch (Exception e)
            {
                Logger.Error($"An error occurred at {stackFrame.IP}: {e.Message}");

                if (e.StackTrace is not null)
                    Logger.Debug(e.StackTrace, true);

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

        nextIP = stackFrame.IP + 1;

        Logger.Debug($"{stackFrame.IP}: {instruction}");

        switch (instruction)
        {
            case Instruction.Syscall instr:
                {
                    switch (instr.Name)
                    {
                        case "GetTime":
                            {
                                stack.Push(new Value.UI64((ulong)DateTime.UtcNow.Ticks));
                            }
                            break;
                        default: throw new Exception($"Unknown Syscall: {instr.Name}");
                    }
                }
                break;
            case Instruction.Exit instr:
                {
                    exitcode = GetSourceOperandValue(stackFrame, instr.Code, new DataType.I64()).As<Value.I64>().Value;
                    State = InterpreterState.ShuttingDown;
                }
                break;
            case Instruction.Store instr: SetDestinationOperandValue(stackFrame, instr.Destination, GetSourceOperandValue(stackFrame, instr.Source)); break;
            case Instruction.Binop instr:
                {
                    var source1 = GetSourceOperandValue(stackFrame, instr.Source1);
                    var source2 = GetSourceOperandValue(stackFrame, instr.Source2);

                    Value result = (instr.Opcode, source1, source2) switch
                    {
                        (Opcode.Add, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 + op2),
                        (Opcode.Mul, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 * op2),
                        (Opcode.Eq, Value.I64(var op1), Value.I64(var op2)) => new Value.Bool(op1 == op2),
                        (Opcode.GetElement, Value.UI64(var index), Value.Array a)
                            => index < (UInt64)a.Elements.Count
                                ? a.Elements[(int)index].Expect(instr.DataType)
                                : throw new Exception($"Index {index} is greater than array length: {a.Elements.Count}"),
                        var operands => throw new Exception($"Cannot apply operation to {operands}.")
                    };

                    SetDestinationOperandValue(stackFrame, instr.Destination, result);
                }
                break;
            case Instruction.Unop instr:
                {
                    var source = GetSourceOperandValue(stackFrame, instr.Source);

                    Value result = (instr.Opcode, source) switch
                    {
                        (Opcode.Dec, Value.I64(var op)) => new Value.I64(op - 1),
                        var operands => throw new Exception($"Cannot apply operation to {operands}.")
                    };

                    SetDestinationOperandValue(stackFrame, instr.Destination, result);
                }
                break;
            case Instruction.Dump instr: Config.StandardOutput.WriteLine(GetSourceOperandValue(stackFrame, instr.Source)); break;
            case Instruction.Assert instr:
                {
                    var value1 = GetSourceOperandValue(stackFrame, instr.Source1);
                    var value2 = GetSourceOperandValue(stackFrame, instr.Source2);

                    if (value1 != value2)
                    {
                        Config.StandardOutput.WriteLine($"Assertion Failed at {stackFrame.IP}: {value1} != {value2}.");

                        exitcode = 1;
                        State = InterpreterState.ShuttingDown;
                    }
                }
                break;
            case Instruction.Call instr:
                {
                    var receivedArgs = instr.Args.Select(source => GetSourceOperandValue(stackFrame, source)).ToArray();
                    PushStackFrame((instr.Module, instr.Function), receivedArgs);
                }
                break;
            case Instruction.Return instr:
                {
                    var values = instr.Sources.Select(source => GetSourceOperandValue(stackFrame, source)).ToArray();
                    PopStackFrame(values);
                }
                break;
            case Instruction.CJump instr:
                {
                    if (GetSourceOperandValue(stackFrame, instr.Condtion, new DataType.Bool()).As<Value.Bool>().Value)
                    {
                        var target = GetFunctionLabelTarget(stackFrame.IP.Module, stackFrame.IP.Function, instr.Label);
                        nextIP = new InstructionPointer(stackFrame.IP.Module, stackFrame.IP.Function, target);
                    }
                }
                break;
            default: throw new Exception($"Instruction '{instruction.Opcode.ToString()}' has not been implemented.");
        }
    }

    private Value GetSourceOperandValue(StackFrame stackFrame, Operand.Source source, DataType? expectedDataType = null)
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
            Operand.Array(var elementType, var elements)
                => new Value.Array(elementType, elements.Select(elem => GetSourceOperandValue(stackFrame, elem, elementType)).ToArray()),
            _ => throw new Exception($"Invalid source: {source}")
        };

        if (expectedDataType is not null && value.DataType != expectedDataType)
            throw new Exception($"Invalid data type for source: {source}. Expected {expectedDataType}, but found {value.DataType}");

        return value;
    }

    private void SetDestinationOperandValue(StackFrame stackFrame, Operand.Destination destination, Value value, DataType? expectedDataType = null)
    {
        Action setter;
        DataType destDataType;

        switch (destination)
        {
            case Operand.Local.Indexed l:
                {
                    var local = stackFrame.Locals[l.Index];

                    if (value.DataType != local.DataType)
                        throw new InvalidCastException($"Cannot set local to {value}. Expected {local.DataType}.");

                    setter = delegate { stackFrame.Locals[l.Index] = value; };
                    destDataType = local.DataType;
                }
                break;
            case Operand.Local.Named l:
                {
                    var local = stackFrame.GetLocal(l.Name);

                    if (value.DataType != local.DataType)
                        throw new InvalidCastException($"Cannot set local to {value}. Expected {local.DataType}.");

                    setter = delegate { stackFrame.SetLocal(l.Name, value); };
                    destDataType = local.DataType;
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

        if (expectedDataType is not null)
        {
            if (value.DataType != expectedDataType) { throw new Exception($"Expected {expectedDataType}, but found {value}"); }
            else if (destDataType != expectedDataType) { throw new Exception($"Invalid data type for destination: {destination}. Expected {expectedDataType}, but found {destDataType}"); }
        }

        setter();
    }

    private Value PopStack() => stack.Count != 0 ? stack.Pop() : throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

    private T PopStack<T>() where T : Value
    {
        if (!stack.TryPeek(out var stackTop))
            throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

        if (stackTop is not T)
            throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");

        return (T)stack.Pop();
    }

    private Value PopStack(DataType dt)
    {
        if (!stack.TryPeek(out var stackTop))
            throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

        if (stackTop.DataType != dt)
            throw new InvalidCastException($"Unable to pop '{dt}' off stack. Found '{stackTop.DataType}'");

        return stack.Pop();
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

    public Function GetFunction(string module, string function) => Program.GetModule(module).GetFunction(function);
    private Instruction GetInstruction(InstructionPointer ip) => Program.GetModule(ip.Module).GetFunction(ip.Function).Instructions[(int)ip.Index];
    public UInt64 GetFunctionLabelTarget(string module, string function, string label) => Program.GetModule(module).GetFunction(function).Labels[label].Target;
}