using Komodo.Compilation.Bytecode;

namespace Komodo.Interpretation;

public class StackFrame
{
    public InstructionPointer IP;

    public int FramePointer { get; }
    public Value[] Arguments { get; }
    public Value?[] Locals { get; }

    public StackFrame(InstructionPointer ip, int fp, IEnumerable<Value> args, IEnumerable<Value?> locals)
    {
        IP = ip;
        FramePointer = fp;
        Arguments = args.ToArray();
        Locals = locals.ToArray();
    }
}