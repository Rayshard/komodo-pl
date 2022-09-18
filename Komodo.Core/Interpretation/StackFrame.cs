using System.Collections.ObjectModel;
using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public class StackFrame
{
    public InstructionPointer IP;

    public int FramePointer { get; }
    public ReadOnlyCollection<Operand.Destination> ReturnDests { get; }
    public ReadOnlyCollection<Value> Arguments { get; }
    public ReadOnlyDictionary<string, int> NamedArguments { get; }
    public ReadOnlyDictionary<string, int> NamedLocals { get; }
    public Value[] Locals { get; }

    public StackFrame(InstructionPointer ip, int fp, IEnumerable<(Value Value, string? Name)> arguments, IEnumerable<(Value Value, string? Name)> locals, IEnumerable<Operand.Destination> returnDests)
    {
        IP = ip;
        FramePointer = fp;
        ReturnDests = new ReadOnlyCollection<Operand.Destination>(returnDests.ToArray());

        (Arguments, NamedArguments) = arguments.ToCollectionWithMap(item => item.Name, item => item.Value);

        var (localsArray, localsMap) = locals.ToCollectionWithMap(item => item.Name, item => item.Value);
        (Locals, NamedLocals) = (localsArray.ToArray(), localsMap);
    }
}