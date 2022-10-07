using System.Collections.ObjectModel;
using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public class StackFrame
{
    public InstructionPointer IP;
    public InstructionPointer? LastIP;

    public int FramePointer { get; }
    public ReadOnlyCollection<Value> Arguments { get; }
    public ReadOnlyDictionary<string, int> NamedArguments { get; }
    public ReadOnlyDictionary<string, int> NamedLocals { get; }
    public Value[] Locals { get; }

    public StackFrame(InstructionPointer ip, int fp, IEnumerable<(Value Value, string? Name)> arguments, IEnumerable<(Value Value, string? Name)> locals)
    {
        IP = ip;
        FramePointer = fp;

        (Arguments, NamedArguments) = arguments.ToCollectionWithMap(item => item.Name, item => item.Value);

        var (localsArray, localsMap) = locals.ToCollectionWithMap(item => item.Name, item => item.Value);
        (Locals, NamedLocals) = (localsArray.ToArray(), localsMap);
    }

    public Value GetArgument(string name) => Arguments[NamedArguments[name]];
    public Value GetLocal(string name) => Locals[NamedLocals[name]];
    public void SetLocal(string name, Value value) => Locals[NamedLocals[name]] = value;
}