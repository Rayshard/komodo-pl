namespace Komodo.Compilation.Bytecode;

public class Module
{
    public string Name { get; }

    private Dictionary<string, Function> functions = new Dictionary<string, Function>();
    public IEnumerable<Function> Functions => functions.Values;

    public Module(string name) => Name = name;

    public void AddFunction(Function function) => functions.Add(function.Name, function);
    public Function GetFunction(string name) => functions[name];
}