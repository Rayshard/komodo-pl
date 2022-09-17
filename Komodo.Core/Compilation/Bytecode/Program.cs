using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public class Program
{
    public string Name { get; }
    public (string Module, string Function) Entry { get; }

    private Dictionary<string, Module> modules = new Dictionary<string, Module>();
    public IEnumerable<Module> Modules => modules.Values;
    public IEnumerable<(string Module, Function Function)> Functions => Modules.Select(m => m.Functions.Select(f => (m.Name, f))).SelectMany(f => f);

    public Program(string name, (string Module, string Function) entry)
    {
        Name = name;
        Entry = entry;
    }

    public void AddModule(Module module) => modules.Add(module.Name, module);
    public Module GetModule(string name) => modules[name];

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

        nodes.AddRange(Modules.Select(module => module.AsSExpression()));

        return new SExpression.List(nodes);
    }

    public static Program Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(3, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("program");

        var name = list[1].ExpectUnquotedSymbol().Value;

        var entryNode = list[2].ExpectList().ExpectLength(3);
        entryNode.ElementAt(0).ExpectUnquotedSymbol().ExpectValue("entry");

        var entryModule = entryNode.ElementAt(1).ExpectUnquotedSymbol().Value;
        var entryFunction = entryNode.ElementAt(2).ExpectUnquotedSymbol().Value;
        var program = new Program(name, (entryModule, entryFunction));

        foreach (var item in list.Skip(3))
            program.AddModule(Module.Deserialize(item));

        return program;
    }

}