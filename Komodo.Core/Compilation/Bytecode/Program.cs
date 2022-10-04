using System.Collections.ObjectModel;
using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public record DataItem(string Name, Byte[] Bytes)
{
    public static DataItem Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList()
                        .ExpectLength(2)
                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().Value, out var name)
                        .ExpectItem(1, item => Encoding.UTF8.GetBytes(item.ExpectQuotedSymbol().Value), out var bytes);

        return new DataItem(name, bytes);
    }
}

public class Program
{
    public string Name { get; }
    public (string Module, string Function) Entry { get; }

    private Dictionary<string, Module> modules = new Dictionary<string, Module>();
    public IEnumerable<Module> Modules => modules.Values;

    public ReadOnlyDictionary<string, DataItem> DataSegemnt { get; }

    public IEnumerable<(string Module, Function Function)> Functions => Modules.Select(m => m.Functions.Select(f => (m.Name, f))).SelectMany(f => f);

    public Program(string name, (string Module, string Function) entry, IEnumerable<DataItem> dataItems)
    {
        Name = name;
        Entry = entry;
        DataSegemnt = new ReadOnlyDictionary<string, DataItem>(dataItems.ToDictionary(item => item.Name));
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

        // Deserialize globals
        var dataItems = new DataItem[0];

        if (remaining.Count() > 0
            && remaining.First() is SExpression.List dataSegment
            && dataSegment.Count() >= 1
            && dataSegment[0] is SExpression.UnquotedSymbol dataSegmentStartSymbol
            && dataSegmentStartSymbol.Value == "data")
        {
            dataItems = dataSegment.Skip(1).Select(DataItem.Deserialize).ToArray();
            remaining = remaining.Skip(1);
        }

        var program = new Program(name, (entryModule, entryFunction), dataItems);

        foreach (var item in remaining)
            program.AddModule(Module.Deserialize(item));

        return program;
    }

}