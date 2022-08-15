using Komodo.Compilation.Bytecode;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public enum ThreadState { NotStarted, Running, Exited }

public interface Signal
{
    public record Terminate() : Signal;
}

public class Thread
{
    public Interpreter Interpreter { get; }
    public InstructionPointer IP { get; private set; }
    public ThreadState State { get; private set; }

    private Stack<Value> stack = new Stack<Value>();
    private Queue<Signal> signals = new Queue<Signal>();

    public Thread(Interpreter interpreter, InstructionPointer start)
    {
        Interpreter = interpreter;
        IP = start;
        State = ThreadState.NotStarted;
    }

    public void Run()
    {
        State = ThreadState.Running;

        while (!signals.OfType<Signal.Terminate>().Any())
        {
            var instruction = Interpreter.Program.GetModule(IP.Module).GetFunction(IP.Function).GetBasicBlock(IP.BasicBlock)[IP.Index];
            var nextIP = new InstructionPointer(IP.Module, IP.Function, IP.BasicBlock, IP.Index + 1);

            Logger.Debug($"{IP}: {instruction}");

            switch (instruction)
            {
                case Instruction.Syscall instr:
                    {
                        switch (instr.Code)
                        {
                            case SyscallCode.ExitProcess: Interpreter.Request(new Request.Exit(this, 1)); break;
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

            Console.WriteLine(StackToString());

            IP = nextIP;
        }

        State = ThreadState.Exited;
    }

    private T PopStack<T>() where T : class
    {
        var stackTop = stack.Peek();
        if (stackTop is not T)
            throw new InvalidCastException($"Unable to pop '{typeof(T)}' off stack. Found '{stackTop.GetType()}'");

        return (T)stack.Pop();
    }

    public string StackToString() => String.Join('\n', stack.Reverse().Select(value => $"{value}").ToArray());

    public void Send(Signal signal) => signals.Enqueue(signal);
}