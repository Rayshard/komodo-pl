namespace Komodo.Compilation.Bytecode;

public class Program
{
    public string Name { get; }
    public (string Module, string Function) Entry { get; }

    private Dictionary<string, Module> modules = new Dictionary<string, Module>();
    public IEnumerable<Module> Modules => modules.Values;

    public Program(string name, (string Module, string Function) entry)
    {
        Name = name;
        Entry = entry;
    }

    public void AddModule(Module module) => modules.Add(module.Name, module);
    public Module GetModule(string name) => modules[name];
}