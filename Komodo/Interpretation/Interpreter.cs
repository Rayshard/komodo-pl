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
        PushStackFrame(entryIP, new Value[] { }, 0);

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
                case Instruction.AddI64:
                    {
                        var op1 = PopStack<Value.I64>().Value;
                        var op2 = PopStack<Value.I64>().Value;

                        stack.Push(new Value.I64(op1 + op2));
                    }
                    break;
                    case Instruction.PrintI64:
                    {
                        var value = PopStack<Value.I64>().Value;
                        Console.WriteLine(value);
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

    private T PopStack<T>()
    {
        var stackTop = stack.Peek();
        if (stackTop is not T)
            throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");

        return (T)stack.Pop();
    }

    private void PushStackFrame(InstructionPointer start, IEnumerable<Value> args, int numLocals)
        => callStack.Push(new StackFrame(start, stack.Count, args, new Value?[numLocals]));

    private void PopStackFrame(Value? returnValue)
    {
        if (callStack.TryPeek(out var frame))
        {
            while (stack.Count > frame.FramePointer)
                stack.Pop();

            if (returnValue is not null)
                stack.Push(returnValue);
        }
        else { throw new InvalidOperationException("Cannot pop stack frame. The call stack is empty."); }
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());
}