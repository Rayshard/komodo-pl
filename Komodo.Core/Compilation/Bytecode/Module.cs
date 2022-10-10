using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record Import(string Name)
{
    public record Function(string Name, VSROCollection<OptionallyNamedDataType> Parameters, VSROCollection<DataType> Returns) : Import(Name)
    {
        new public static Function Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(4, null)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("function"))
                 .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name)
                 .ExpectItem(2,
                            item => item.ExpectList()
                                        .ExpectLength(1, null)
                                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("params")),
                            out var paramsList)
                 .ExpectItem(3,
                            item => item.ExpectList()
                                        .ExpectLength(1, null)
                                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("returns")),
                            out var returnsList);

            paramsList.ExpectItems(item => OptionallyNamedDataType.Deserialize(item), out var parameters, 1);
            returnsList.ExpectItems(item => DataType.Deserialize(item), out var returns, 1);

            return new Function(name, parameters.ToVSROCollection(), returns.ToVSROCollection());
        }
    }

    private static Func<SExpression, Import>[] Deserializers => new Func<SExpression, Import>[] {
        Function.Deserialize
    };

    public static Import Deserialize(SExpression sexpr)
    {
        foreach (var deserializer in Deserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid import: {sexpr}", sexpr);
    }
}

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
    VSRODictionary<string, Import> Imports,
    VSRODictionary<string, Data> Data,
    VSRODictionary<string, Global> Globals,
    VSRODictionary<string, Function> Functions
)
{
    public bool HasImport(string name) => Imports.ContainsKey(name);
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
    public Dictionary<string, Import> imports = new Dictionary<string, Import>();
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

        // Deserialize imports
        if (remaining.Count() > 0
            && remaining.First() is SExpression.List importNode
            && importNode.Count() >= 1
            && importNode[0] is SExpression.UnquotedSymbol importNodeStartSymbol
            && importNodeStartSymbol.Value == "import")
        {
            importNode.Skip(1).Select(Import.Deserialize).ForEach(AddImport);
            remaining = remaining.Skip(1);
        }

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

    public void AddImport(Import import) => imports.Add(import.Name, import);
    public bool HasImport(string name) => data.ContainsKey(name);
    public Import GetImport(string name) => imports[name];

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
        var imports = this.imports.ToVSRODictionary();
        var data = this.data.ToVSRODictionary();
        var globals = this.globals.ToVSRODictionary();
        var functions = this.functions.ToVSRODictionary();

        return new Module(name, imports, data, globals, functions);
    }
}