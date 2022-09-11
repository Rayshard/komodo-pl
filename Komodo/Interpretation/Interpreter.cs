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
            var instruction = FetchInstruction(stackFrame.IP);

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
                case Instruction.PushI64 instr: stack.Push(new Value.I64(instr.Value)); break;
                case Instruction.Add:
                    {
                        Value result = (PopStack(), PopStack()) switch
                        {
                            (Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 + op2),
                            var operands => throw new Exception($"Cannot apply operation to {operands}.")
                        };

                        stack.Push(result);
                    }
                    break;
                case Instruction.Mul:
                    {
                        Value result = (PopStack(), PopStack()) switch
                        {
                            (Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 * op2),
                            var operands => throw new Exception($"Cannot apply operation to {operands}.")
                        };

                        stack.Push(result);
                    }
                    break;
                case Instruction.Eq:
                    {
                        Value result = (PopStack(), PopStack()) switch
                        {
                            (Value.I64(var op1), Value.I64(var op2)) => new Value.I64(op1 == op2 ? 1 : 0),
                            var operands => throw new Exception($"Cannot apply operation to {operands}.")
                        };

                        stack.Push(result);
                    }
                    break;
                case Instruction.Dec:
                    {
                        Value result = PopStack() switch
                        {
                            Value.I64(var value) => new Value.I64(value - 1),
                            var operand => throw new Exception($"Cannot apply operation to {operand.DataType}.")
                        };

                        stack.Push(result);
                    }
                    break;
                case Instruction.Print:
                    {
                        var value = PopStack();

                        switch (value)
                        {
                            case Value.I64(var i64): Console.WriteLine(i64); break;
                            default: throw new Exception($"Cannot apply operation to {value.DataType}.");
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
                case Instruction.Return instr:
                    {
                        var expectedReturnType = GetFunctionFromIP(stackFrame.IP).ReturnType;
                        PopStackFrame(expectedReturnType == DataType.Unit ? null : PopStack(expectedReturnType));
                    }
                    break;
                case Instruction.LoadArg instr: stack.Push(stackFrame.Arguments[instr.Index]); break;
                case Instruction.JNZ instr:
                    {
                        bool nonzero = PopStack() switch
                        {
                            Value.I64(var value) => value != 0,
                            var operand => throw new Exception($"Cannot apply operation to {operand.DataType}.")
                        };

                        if (nonzero)
                            stackFrame.IP = new InstructionPointer(stackFrame.IP.Module, stackFrame.IP.Function, instr.BasicBlock, -1);
                    }
                    break;
                default: throw new NotImplementedException(instruction.Opcode.ToString());
            }

            Logger.Debug(StackToString(), startOnNewLine: true);
            stackFrame.IP = stackFrame.IP + 1;
        }

        State = InterpreterState.Terminated;
        return exitcode;
    }

    private Instruction FetchInstruction(InstructionPointer ip)
        => Program.GetModule(ip.Module).GetFunction(ip.Function).GetBasicBlock(ip.BasicBlock)[ip.Index];

    private Value PopStack() => stack.Count != 0 ? stack.Pop() : throw new InvalidOperationException("Cannot pop value from stack. The stack is empty.");

    private T PopStack<T>()
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

    private void PopStackFrame(Value? returnValue)
    {
        if (callStack.TryPop(out var frame))
        {
            while (stack.Count > frame.FramePointer)
                stack.Pop();

            if (returnValue is not null)
                stack.Push(returnValue);
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());

    public Function GetFunctionFromIP(InstructionPointer ip) => Program.GetModule(ip.Module).GetFunction(ip.Function);
}