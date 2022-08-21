namespace Komodo.Interpretation;

public class StackFrame
{
    public InstructionPointer IP { get; }
    public InstructionPointer? ReturnIP { get; }

    private Value[] arguments;
    public IEnumerable<Value> Arguments => arguments;

    private Dictionary<string, Value?> locals = new Dictionary<string, Value?>();

    public StackFrame(InstructionPointer ip, InstructionPointer? returnIP, IEnumerable<Value> args, HashSet<string> locals)
    {
        IP = ip;
        ReturnIP = returnIP;
        arguments = args.ToArray();

        foreach (var name in locals)
            this.locals.Add(name, null);
    }

    public Value? GetLocal(string name) => locals[name];
}