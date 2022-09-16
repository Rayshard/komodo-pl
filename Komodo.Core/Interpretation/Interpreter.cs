using Komodo.Compilation.Bytecode;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public enum InterpreterState { NotStarted, Running, ShuttingDown, Terminated }

public class Interpreter
{
    public InterpreterState State { get; private set; }
    public Program Program { get; }

    private Stack<Value> stack = new Stack<Value>();
    private Stack<StackFrame> callStack = new Stack<StackFrame>();

    public Interpreter(Program program)
    {
        State = InterpreterState.NotStarted;
        Program = program;
    }

    public Int64 Run()
    {
        Int64 exitcode = 0;
        int numInstructionsExecuted = 0;

        State = InterpreterState.Running;

        // Call Program Entry
        PushStackFrame(Program.Entry, new Value[] { }, Program.GetModule(Program.Entry.Module).GetFunction(Program.Entry.Function).Locals, new Operand.Destination[] { });

        while (State == InterpreterState.Running)
        {
            if (callStack.Count == 0)
                throw new Exception("Exited with an empty call stack");

            var stackFrame = callStack.Peek();

            try
            {
                ExecuteNextInstruction(stackFrame, ref exitcode);
                numInstructionsExecuted++;

                var stackAsString = StackToString();
                stackAsString = stackAsString.Length == 0 ? " Empty" : ("\n" + stackAsString);

                Logger.Debug($"Current Stack:{stackAsString}");
                stackFrame.IP = stackFrame.IP + 1;
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

    private void ExecuteNextInstruction(StackFrame stackFrame, ref Int64 exitcode)
    {
        var instruction = GetInstructionFromIP(stackFrame.IP);

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
            case Instruction.Print instr:
                {
                    switch (GetSourceOperandValue(stackFrame, instr.Source, instr.DataType))
                    {
                        case Value.I64(var i): Console.WriteLine(i); break;
                        case Value.Bool(var b): Console.WriteLine(b ? "true" : "false"); break;
                        default: throw new Exception($"Cannot apply operation to {instr.DataType}.");
                    }
                }
                break;
            case Instruction.Assert instr:
                {
                    var value1 = GetSourceOperandValue(stackFrame, instr.Source1);
                    var value2 = GetSourceOperandValue(stackFrame, instr.Source2);

                    if (value1 != value2)
                    {
                        Console.WriteLine($"Assertion Failed at {stackFrame.IP}: {value1} != {value2}.");

                        exitcode = 1;
                        State = InterpreterState.ShuttingDown;
                    }
                }
                break;
            case Instruction.Call instr:
                {
                    var function = GetFunctionFromIP(new InstructionPointer(instr.Module, instr.Function, Function.ENTRY_NAME, 0));

                    // Verify args
                    if (instr.Args.Count() != function.Arguments.Count())
                        throw new Exception($"Function {instr.Module}.{instr.Function} expects {function.Arguments.Count()} but got {instr.Args.Count()}.");

                    var receivedArgs = instr.Args.Select((source, i) => GetSourceOperandValue(stackFrame, source, function.Arguments.ElementAt(i)));

                    //Verify return destinations
                    if (instr.Returns.Count() != function.Returns.Count())
                        throw new Exception($"Function {instr.Module}.{instr.Function} returns {function.Returns.Count()} values, but got {instr.Returns.Count()} return destinations.");

                    PushStackFrame((instr.Module, instr.Function), receivedArgs, function.Locals, instr.Returns);
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
                    if (GetSourceOperandValue<Value.Bool>(stackFrame, instr.Condtion).Value)
                        stackFrame.IP = new InstructionPointer(stackFrame.IP.Module, stackFrame.IP.Function, instr.BasicBlock, -1);
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
            Operand.Local(var i) => stackFrame.GetLocal(i),
            Operand.Arg(var i) => stackFrame.GetArg(i),
            Operand.Stack => PopStack(),
            _ => throw new Exception($"Invalid source: {source}")
        };

        if (expectedDataType.HasValue && value.DataType != expectedDataType.Value)
            throw new Exception($"Invalid data type for source: {source}. Expected {expectedDataType}, but found {value.DataType}");

        return value;
    }

    private T GetSourceOperandValue<T>(StackFrame stackFrame, Operand.Source source) where T : Value
        => (T)GetSourceOperandValue(stackFrame, source, Value.GetDataType<T>());

    private void SetDestinationOperandValue(StackFrame stackFrame, Operand.Destination destination, Value value, DataType? expectedDataType = null)
    {
        Action setter;
        DataType destDataType;

        switch (destination)
        {
            case Operand.Local l:
                {
                    var local = stackFrame.GetLocal(l.Index);

                    if (value.DataType != local.DataType)
                        throw new InvalidCastException($"Cannot set local to {value}. Expected {local.DataType}.");

                    setter = delegate { stackFrame.SetLocal(l.Index, value); };
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

        if (expectedDataType.HasValue)
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
                    var localDataType = stackFrame.GetLocal(l.Index).DataType;
                    return localDataType == dataType ? destination : throw new Exception($"Destination '{l}' expects {localDataType}, but got {dataType}.");
                }
            case Operand.Stack: return destination;
            default: throw new Exception($"Invalid destination: {destination}");
        };
    }

    private bool DestinationAccepts(StackFrame stackFrame, Operand.Destination destination, DataType? dataType) => destination switch
    {
        Operand.Local l => stackFrame.GetLocal(l.Index).DataType == dataType,
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

    private void PushStackFrame((string Module, string Function) Target, IEnumerable<Value> args, IEnumerable<DataType> locals, IEnumerable<Operand.Destination> returnDests)
    {
        var start = new InstructionPointer(Target.Module, Target.Function, Function.ENTRY_NAME, 0);
        var localDefaultValues = locals.Select(l => Value.CreateDefault(l));

        callStack.Push(new StackFrame(start, stack.Count, args, localDefaultValues, returnDests));
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

            foreach(var (value, dest) in returnValues.Zip(frame.ReturnDests).Reverse())
                SetDestinationOperandValue(parentStackFrame, dest, value);
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());

    public Function GetFunctionFromIP(InstructionPointer ip) => Program.GetModule(ip.Module).GetFunction(ip.Function);

    private Instruction GetInstructionFromIP(InstructionPointer ip)
        => Program.GetModule(ip.Module).GetFunction(ip.Function).GetBasicBlock(ip.BasicBlock)[ip.Index];
}