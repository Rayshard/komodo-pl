namespace Komodo.Compilation.Bytecode;

public class Module
{
    private Dictionary<string, Function> functions = new Dictionary<string, Function>();

    public Program Parent { get; }
    public string Name { get; }

    public IEnumerable<Function> Functions => functions.Values;

    public Module(Program parent, string name)
    {
        Parent = parent;
        Name = name;
    }

    public Function CreateFunction(string name)
    {
        functions.Add(name, new Function(this, name));
        return GetFunction(name);
    }

    public Function GetFunction(string name) => functions[name];
}