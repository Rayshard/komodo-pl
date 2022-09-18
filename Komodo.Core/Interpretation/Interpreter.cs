using System.Collections.ObjectModel;
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

    private Stack<Value> stack = new Stack<Value>();
    private Stack<StackFrame> callStack = new Stack<StackFrame>();

    public Interpreter(Program program, InterpreterConfig config)
    {
        State = InterpreterState.NotStarted;
        Program = program;
        Config = config;
    }

    public Int64 Run()
    {
        Int64 exitcode = 0;
        int numInstructionsExecuted = 0;

        State = InterpreterState.Running;

        // Call Program Entry
        var entryArgs = new ReadOnlyCollection<Value>(new Value[0]);
        var entryReturnDests = new ReadOnlyCollection<Operand.Destination>(new Operand.Destination[0]);
        PushStackFrame(Program.Entry, entryArgs, entryReturnDests);

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
        var instruction = GetInstructionFromIP(stackFrame.IP);

        nextIP = stackFrame.IP + 1;

        Logger.Debug($"{stackFrame.IP}: {instruction}");

        switch (instruction)
        {
            case Instruction.Syscall instr:
                {
                    switch (instr.Name)
                    {
                        case "Exit":
                            {
                                exitcode = PopStack<Value.I64>().Value;
                                State = InterpreterState.ShuttingDown;
                            }
                            break;
                        default: throw new Exception($"Unknown Syscall: {instr.Name}");
                    }
                }
                break;
            case Instruction.Load instr: stack.Push(GetSourceOperandValue(stackFrame, instr.Source)); break;
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
            case Instruction.Dec instr:
                {
                    Value result = GetSourceOperandValue(stackFrame, instr.Source, instr.DataType) switch
                    {
                        Value.I64(var value) => new Value.I64(value - 1),
                        var operand => throw new Exception($"Cannot apply operation to {operand.DataType}.")
                    };

                    SetDestinationOperandValue(stackFrame, instr.Destination, result, instr.DataType);
                }
                break;
            case Instruction.Print instr: Config.StandardOutput.WriteLine(GetSourceOperandValue(stackFrame, instr.Source, instr.DataType)); break;
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
                    var function = GetFunctionFromIP(new InstructionPointer(instr.Module, instr.Function, 0));
                    var receivedArgs = instr.Args.Select((source, i) => GetSourceOperandValue(stackFrame, source, function.Parameters[i].DataType)).ToArray();

                    PushStackFrame((instr.Module, instr.Function), new ReadOnlyCollection<Value>(receivedArgs), instr.Returns);
                }
                break;
            case Instruction.Return instr:
                {
                    var function = GetFunctionFromIP(stackFrame.IP);

                    if (instr.Sources.Count() != function.Returns.Count())
                        throw new Exception($"Expected {function.Returns.Count()} return values but got {instr.Sources.Count()}.");

                    var returnValues = instr.Sources.Zip(function.Returns).Select(item => GetSourceOperandValue(stackFrame, item.First, item.Second)).ToArray();

                    PopStackFrame(returnValues);
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
            Operand.Local(var i) => stackFrame.Locals[(int)i],
            Operand.Arg(var i) => stackFrame.Arguments[(int)i],
            Operand.Stack => PopStack(),
            Operand.Array(var elementType, var elements)
                => new Value.Array(elementType, elements.Select(elem => GetSourceOperandValue(stackFrame, elem, elementType)).ToList()),
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
            case Operand.Local l:
                {
                    var local = stackFrame.Locals[l.Index];

                    if (value.DataType != local.DataType)
                        throw new InvalidCastException($"Cannot set local to {value}. Expected {local.DataType}.");

                    setter = delegate { stackFrame.Locals[l.Index] = value; };
                    destDataType = local.DataType;
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

    private Operand.Destination ExpectDestination(StackFrame stackFrame, Operand.Destination destination, DataType? dataType)
    {
        switch (destination)
        {
            case Operand.Local l:
                {
                    var localDataType = stackFrame.Locals[l.Index].DataType;
                    return localDataType == dataType ? destination : throw new Exception($"Destination '{l}' expects {localDataType}, but got {dataType}.");
                }
            case Operand.Stack: return destination;
            default: throw new Exception($"Invalid destination: {destination}");
        };
    }

    private bool DestinationAccepts(StackFrame stackFrame, Operand.Destination destination, DataType? dataType) => destination switch
    {
        Operand.Local l => stackFrame.Locals[l.Index].DataType == dataType,
        Operand.Stack => true,
        _ => throw new Exception($"Invalid destination: {destination}")
    };

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

    private void PushStackFrame((string Module, string Function) Target, ReadOnlyCollection<Value> args, ReadOnlyCollection<Operand.Destination> returnDests)
    {
        var start = new InstructionPointer(Target.Module, Target.Function, 0);
        var function = GetFunctionFromIP(start);

        if (args.Count != function.Parameters.Count)
            throw new Exception($"Target function required {function.Parameters.Count} arguments, but only {args.Count} were given!");

        if (returnDests.Count != function.Returns.Count)
            throw new Exception($"Function {Target.Module}.{Target.Function} returns {function.Returns.Count} values, but got {returnDests.Count} return destinations.");

        var arguments = args.Select((a, i) => (a, function.Parameters[i].Name)).ToArray();
        var locals = function.Locals.Select(l => (Value.CreateDefault(l.DataType), l.Name)).ToArray();

        callStack.Push(new StackFrame(start, stack.Count, arguments, locals, returnDests));
    }

    private void PopStackFrame(Value[] returnValues)
    {
        if (callStack.TryPop(out var frame))
        {
            while (stack.Count > frame.FramePointer)
                stack.Pop();

            var parentStackFrame = callStack.Peek();

            if (returnValues.Length != frame.ReturnDests.Count())
                throw new Exception($"Parent stack frame expected {frame.ReturnDests} return values but got {returnValues.Length}.");

            foreach (var (value, dest) in returnValues.Zip(frame.ReturnDests).Reverse())
                SetDestinationOperandValue(parentStackFrame, dest, value);
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());

    public Function GetFunctionFromIP(InstructionPointer ip) => Program.GetModule(ip.Module).GetFunction(ip.Function);

    private Instruction GetInstructionFromIP(InstructionPointer ip)
        => Program.GetModule(ip.Module).GetFunction(ip.Function).Instructions[(int)ip.Index];

    public UInt64 GetFunctionLabelTarget(string module, string function, string label) => Program.GetModule(module).GetFunction(function).Labels[label].Target;
}