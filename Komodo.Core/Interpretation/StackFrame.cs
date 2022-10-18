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

    private Dictionary<string, Value> locals;

    public StackFrame(InstructionPointer ip, int fp, IEnumerable<(Value Value, string? Name)> arguments, IEnumerable<(string Name, Value Value)> locals)
    {
        IP = ip;
        FramePointer = fp;

        (Arguments, NamedArguments) = arguments.ToCollectionWithMap(item => item.Name, item => item.Value);

        this.locals = locals.ToDictionary();
    }

    public Value GetArgument(string name) => Arguments[NamedArguments[name]];
    public Value GetLocal(string name) => locals[name];
    public void SetLocal(string name, Value value) => locals[name] = value;
}