using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public record Data(string Name, VSROCollection<Byte> Bytes)
{
    public static Data Deserialize(SExpression sexpr)
    {
        sexpr.ExpectList()
             .ExpectLength(2)
             .ExpectItem(0, item => item.ExpectUnquotedSymbol().Value, out var name)
             .ExpectItem(1, item => Encoding.UTF8.GetBytes(item.ExpectQuotedSymbol().Value), out var bytes);

        return new Data(name, bytes.ToVSROCollection());
    }
}

public record Global(string Name, DataType DataType)
{
    public static Global Deserialize(SExpression sexpr)
    {
        sexpr.ExpectList()
             .ExpectLength(2)
             .ExpectItem(0, DataType.Deserialize, out var dataType)
             .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name);

        return new Global(name, dataType);
    }
}

public record Module(
    string Name,
    VSRODictionary<string, Data> Data,
    VSRODictionary<string, Global> Globals,
    VSRODictionary<string, Function> Functions
)
{
    public bool HasData(string name) => Data.ContainsKey(name);
    public bool HasGlobal(string name) => Globals.ContainsKey(name);
    public bool HasFunction(string name) => Functions.ContainsKey(name);

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("module"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));
        nodes.AddRange(Functions.Values.Select(function => function.AsSExpression()));

        return new SExpression.List(nodes);
    }

    public static Module Deserialize(SExpression sexpr) => new ModuleBuilder(sexpr).Build();
}

public class ModuleBuilder
{
    private string? name;
    public Dictionary<string, Data> data = new Dictionary<string, Data>();
    private Dictionary<string, Global> globals = new Dictionary<string, Global>();
    private Dictionary<string, Function> functions = new Dictionary<string, Function>();

    public ModuleBuilder(SExpression sexpr)
    {
        var remaining = sexpr.ExpectList()
                             .ExpectLength(2, null)
                             .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("module"))
                             .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name)
                             .Skip(2);

        SetName(name);

        // Deserialize globals
        if (remaining.Count() > 0
            && remaining.First() is SExpression.List globalsNode
            && globalsNode.Count() >= 1
            && globalsNode[0] is SExpression.UnquotedSymbol globalsNodeStartSymbol
            && globalsNodeStartSymbol.Value == "globals")
        {
            globalsNode.Skip(1).Select(Global.Deserialize).ForEach(AddGlobal);
            remaining = remaining.Skip(1);
        }

        // Deserialize data
        if (remaining.Count() > 0
            && remaining.First() is SExpression.List dataNode
            && dataNode.Count() >= 1
            && dataNode[0] is SExpression.UnquotedSymbol dataNodeStartSymbol
            && dataNodeStartSymbol.Value == "data")
        {
            dataNode.Skip(1).Select(Data.Deserialize).ForEach(AddData);
            remaining = remaining.Skip(1);
        }

        // Deserialize user defined functions
        remaining.Select(Function.Deserialize).ForEach(AddFunction);
    }

    public void SetName(string? value) => name = value;

    public void AddData(Data data) => this.data.Add(data.Name, data);
    public bool HasData(string name) => data.ContainsKey(name);
    public Data GetData(string name) => data[name];

    public void AddGlobal(Global global) => globals.Add(global.Name, global);
    public bool HasGlobal(string name) => globals.ContainsKey(name);
    public Global GetGlobal(string name) => globals[name];

    public void AddFunction(Function function) => functions.Add(function.Name, function);
    public bool HasFunction(string name) => functions.ContainsKey(name);
    public Function GetFunction(string name) => functions[name];

    public Module Build()
    {
        var name = this.name ?? throw new Exception("Name is not set");
        var data = this.data.ToVSRODictionary();
        var globals = this.globals.ToVSRODictionary();
        var functions = this.functions.ToVSRODictionary();

        return new Module(name, data, globals, functions);
    }
}