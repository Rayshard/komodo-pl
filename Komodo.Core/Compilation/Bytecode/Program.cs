using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public record Program
{
    public string Name { get; }
    public (string Module, string Function) Entry { get; }
    public VSRODictionary<string, Module> Modules { get; }

    public IEnumerable<(string Module, Function Function)> Functions => Modules.Select(m => m.Value.Functions.Select(f => (m.Key, f))).Flatten();

    public Program(string name, (string Module, string Function) entry, IEnumerable<Module> modules)
    {
        Name = name;
        Entry = entry;
        Modules = new VSRODictionary<string, Module>(modules, module => module.Name);
    }

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("program"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));

        var entryNode = new SExpression.List(new[]
        {
            new SExpression.UnquotedSymbol("entry"),
            new SExpression.UnquotedSymbol(Entry.Module),
            new SExpression.UnquotedSymbol(Entry.Function),
        });

        nodes.AddRange(Modules.Select(module => module.Value.AsSExpression()));

        return new SExpression.List(nodes);
    }
}

public class ProgramBuilder
{
    private string? name;
    private (string Module, string Function)? entry;
    private Dictionary<string, Module> modules = new Dictionary<string, Module>();

    public void SetName(string? value) => name = value;    

    public void SetEntry((string ModuleName, string FunctionName)? value) => entry = value.HasValue
        ? HasFunction(value.Value.ModuleName, value.Value.FunctionName)
            ? value
            : throw new Exception($"Cannot set entry. Function {value.Value.ModuleName}.{value.Value.FunctionName} does not exist")
        : null;

    public void AddModule(Module module) => modules.Add(module.Name, module);

    public bool HasModule(string name) => modules.ContainsKey(name);
    public bool HasFunction(string moduleName, string functionName) => modules.TryGetValue(moduleName, out var module) && module.HasFunction(functionName);

    public Program Build()
    {
        var name = this.name ?? throw new Exception("Name is not set");
        var entry = this.entry ?? throw new Exception("Entry is not set");

        return new Program(name, entry, modules.Values);
    }

    public static Program Deserialize(SExpression sexpr)
    {
        var remaining = sexpr.ExpectList()
                             .ExpectLength(3, null)
                             .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("program"))
                             .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name)
                             .ExpectItem(2, item => item.ExpectList().ExpectLength(3), out var entryNode)
                             .Skip(3);

        // Deserialize entry
        entryNode.ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("entry"))
                 .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var entryModule)
                 .ExpectItem(2, item => item.ExpectUnquotedSymbol().Value, out var entryFunction);

        var builder = new ProgramBuilder();
        builder.SetName(name);

        foreach (var item in remaining)
            builder.AddModule(Module.Deserialize(item));

        builder.SetEntry((entryModule, entryFunction));
        return builder.Build();
    }

}