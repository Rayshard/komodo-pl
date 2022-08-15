namespace Komodo.Compilation.Bytecode;

public class Program
{
    private Dictionary<string, Module> modules = new Dictionary<string, Module>();

    public string Name { get; }
    public Function Entry { get; }

    public IEnumerable<Module> Modules => modules.Values;

    public Program(string name, (string Module, string Function) entry)
    {
        Name = name;
        Entry = CreateModule(entry.Module).CreateFunction(entry.Function);
    }

    public Module CreateModule(string name)
    {
        modules.Add(name, new Module(this, name));
        return GetModule(name);
    }

    public Module GetModule(string name) => modules[name];
}