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

        State = InterpreterState.Running;

        var entryIP = new InstructionPointer(Program.Entry.Module, Program.Entry.Function, Function.ENTRY_NAME, 0);
        callStack.Push(new StackFrame(entryIP, stack.Count, new Value[] { }, new Value?[] { }));

        while (State == InterpreterState.Running)
        {
            if (callStack.Count == 0)
                throw new Exception("Exited with an empty call stack");

            var stackFrame = callStack.Peek();

            try
            {
                ExecuteNextInstruction(stackFrame, ref exitcode);

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
                    switch (instr.Code)
                    {
                        case SyscallCode.Exit:
                            {
                                exitcode = PopStack<Value.I64>().Value;
                                State = InterpreterState.ShuttingDown;
                            }
                            break;
                        default: throw new NotImplementedException(instr.Code.ToString());
                    }
                }
                break;
            case Instruction.Push instr: stack.Push(instr.Value); break;
            case Instruction.Binop instr:
                {
                    Value result = (instr.Opcode, PopStack(instr.DataType), instr.Value is null ? PopStack(instr.DataType) : instr.Value) switch
                    {
                        (Opcode.Add, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 + op2),
                        (Opcode.Mul, Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 * op2),
                        (Opcode.Eq, Value.I64(var op1), Value.I64(var op2)) => new Value.Bool(op1 == op2),
                        var operands => throw new Exception($"Cannot apply operation to {operands}.")
                    };

                    stack.Push(result);
                }
                break;
            case Instruction.Dec instr:
                {
                    Value result = PopStack(instr.DataType) switch
                    {
                        Value.I64(var value) => new Value.I64(value - 1),
                        var operand => throw new Exception($"Cannot apply operation to {operand.DataType}.")
                    };

                    stack.Push(result);
                }
                break;
            case Instruction.Print instr:
                {
                    var value = PopStack(instr.DataType);

                    switch (value)
                    {
                        case Value.I64(var i): Console.WriteLine(i); break;
                        case Value.Bool(var b): Console.WriteLine(b ? "true" : "false"); break;
                        default: throw new Exception($"Cannot apply operation to {value.DataType}.");
                    }
                }
                break;
            case Instruction.Assert instr:
                {
                    var actual = PopStack();

                    if (actual != instr.Value)
                    {
                        Console.WriteLine($"Assertion Failed at {stackFrame.IP}. Expected {instr.Value}, but found {actual}.");

                        exitcode = 1;
                        State = InterpreterState.ShuttingDown;
                    }
                }
                break;
            case Instruction.Call instr:
                {
                    var function = Program.GetModule(instr.Module).GetFunction(instr.Function);
                    var arguments = function.Arguments.Select(p => PopStack(p));
                    var start = new InstructionPointer(instr.Module, instr.Function, Function.ENTRY_NAME, 0);

                    callStack.Push(new StackFrame(start, stack.Count, arguments, new Value?[function.Locals.Count()]));
                }
                break;
            case Instruction.Return instr: PopStackFrame(GetFunctionFromIP(stackFrame.IP).Returns); break;
            case Instruction.LoadArg instr: stack.Push(stackFrame.Arguments[instr.Index]); break;
            case Instruction.CJump instr:
                {
                    if (PopStack<Value.Bool>().Value)
                        stackFrame.IP = new InstructionPointer(stackFrame.IP.Module, stackFrame.IP.Function, instr.BasicBlock, -1);
                }
                break;
            default: throw new NotImplementedException(instruction.Opcode.ToString());
        }
    }

    private Value PopStack() => stack.Count != 0 ? stack.Pop() : throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

    private T PopStack<T>() where T : Value
    {
        if (stack.Count == 0)
            throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

        var stackTop = stack.Peek();
        if (stackTop is not T)
            throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");

        return (T)stack.Pop();
    }

    private Value PopStack(DataType dt)
    {
        if (stack.Count == 0)
            throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

        var stackTop = stack.Peek();
        if (stackTop.DataType != dt)
            throw new InvalidCastException($"Unable to pop '{dt}' off stack. Found '{stackTop.DataType}'");

        return stack.Pop();
    }

    private void PopStackFrame(IEnumerable<DataType> expectedReturns)
    {
        if (callStack.TryPop(out var frame))
        {
            var returnValues = expectedReturns.Select(PopStack).ToArray();

            while (stack.Count > frame.FramePointer)
                stack.Pop();

            // Push return values on to stack in reverse
            foreach (var value in returnValues.Reverse())
                stack.Push(value);
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());

    public Function GetFunctionFromIP(InstructionPointer ip) => Program.GetModule(ip.Module).GetFunction(ip.Function);

    private Instruction GetInstructionFromIP(InstructionPointer ip)
        => Program.GetModule(ip.Module).GetFunction(ip.Function).GetBasicBlock(ip.BasicBlock)[ip.Index];
}