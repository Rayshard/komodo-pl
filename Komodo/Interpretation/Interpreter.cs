using Komodo.Compilation.Bytecode;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public enum InterpreterState { NotStarted, Running, ShuttingDown, Terminated }

public class Interpreter
{
    public InterpreterState State { get; private set; }
    public Program Program { get; }
    public InstructionPointer IP { get; private set; }

    private Stack<Value> stack = new Stack<Value>();

    public Interpreter(Program program)
    {
        State = InterpreterState.NotStarted;
        Program = program;
        IP = new InstructionPointer(Program.Entry.Module, Program.Entry.Function, Function.ENTRY_NAME, 0);
    }

    public Int64 Run()
    {
        Int64 exitcode = 0;

        State = InterpreterState.Running;

        while (State == InterpreterState.Running)
        {
            var instruction = Program.GetModule(IP.Module).GetFunction(IP.Function).GetBasicBlock(IP.BasicBlock)[IP.Index];
            var nextIP = new InstructionPointer(IP.Module, IP.Function, IP.BasicBlock, IP.Index + 1);

            Logger.Debug($"{IP}: {instruction}");

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
                default: throw new NotImplementedException(instruction.Opcode.ToString());
            }

            Logger.Debug(StackToString(), startOnNewLine: true);
            IP = nextIP;
        }

        State = InterpreterState.Terminated;
        return exitcode;
    }

    private T PopStack<T>()
    {
        var stackTop = stack.Peek();
        if (stackTop is not T)
            throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");

        return (T)stack.Pop();
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());
}