using Komodo.Compilation.Bytecode;

namespace Komodo.Interpretation;

public class StackFrame
{
    public InstructionPointer IP;

    public int FramePointer { get; }
    public IEnumerable<Operand.Destination> ReturnDests;

    private Value[] arguments, locals;

    public StackFrame(InstructionPointer ip, int fp, IEnumerable<Value> args, IEnumerable<Value> locals, IEnumerable<Operand.Destination> returnDests)
    {
        IP = ip;
        FramePointer = fp;
        ReturnDests = returnDests;

        this.arguments = args.ToArray();
        this.locals = locals.ToArray();
    }

    public Value GetLocal(UInt64 index)
    {
        try { return locals[index]; }
        catch (IndexOutOfRangeException) { throw new Exception($"Invalid index '{index}' for local."); }
    }

    public Value GetArg(UInt64 index)
    {
        try { return arguments[index]; }
        catch (IndexOutOfRangeException) { throw new Exception($"Invalid index '{index}' for arg."); }
    }

    public void SetLocal(UInt64 index, Value value)
    {
        try { locals[index] = value; }
        catch (IndexOutOfRangeException) { throw new Exception($"Invalid index '{index}' for local."); }
    }
}