using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

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

        var program = new Program(name, (entryModule, entryFunction));

        foreach (var item in remaining)
            program.AddModule(Module.Deserialize(item));

        return program;
    }

}