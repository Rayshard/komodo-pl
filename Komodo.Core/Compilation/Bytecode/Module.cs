using System.Collections.ObjectModel;
using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public record Data(string Name, Byte[] Bytes)
{
    public static Data Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList()
                        .ExpectLength(2)
                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().Value, out var name)
                        .ExpectItem(1, item => Encoding.UTF8.GetBytes(item.ExpectQuotedSymbol().Value), out var bytes);

        return new Data(name, bytes);
    }
}

public record Global(string Name, DataType DataType, Value? DefaultValue = null)
{
    public static Global Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList()
                        .ExpectLength(2, 3)
                        .ExpectItem(0, DataType.Deserialize, out var dataType)
                        .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name);

        var defaultValue = list.Count() == 2 ? null : list[2].Expect(Value.Deserialize);
        return new Global(name, dataType, defaultValue);
    }
}

public class Module
{
    public string Name { get; }

    public ReadOnlyDictionary<string, Global> Globals { get; }
    public ReadOnlyDictionary<string, Data> Data { get; }

    private Dictionary<string, Function> functions = new Dictionary<string, Function>();
    public IEnumerable<Function> Functions => functions.Values;

    public Module(string name, IEnumerable<Global> globals, IEnumerable<Data> data)
    {
        Name = name;
        Globals = new ReadOnlyDictionary<string, Global>(globals.ToDictionary(item => item.Name));
        Data = new ReadOnlyDictionary<string, Data>(data.ToDictionary(item => item.Name));
    }

    public void AddFunction(Function function) => functions.Add(function.Name, function);
    public Function GetFunction(string name) => functions[name];

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("module"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));
        nodes.AddRange(Functions.Select(function => function.AsSExpression()));

        return new SExpression.List(nodes);
    }

    public static Module Deserialize(SExpression sexpr)
    {
        var remaining = sexpr.ExpectList()
                             .ExpectLength(2, null)
                             .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("module"))
                             .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name)
                             .Skip(2);

        // Deserialize globals
        var globals = new Global[0];

        if (remaining.Count() > 0
            && remaining.First() is SExpression.List globalsNode
            && globalsNode.Count() >= 1
            && globalsNode[0] is SExpression.UnquotedSymbol globalsNodeStartSymbol
            && globalsNodeStartSymbol.Value == "globals")
        {
            globals = globalsNode.Skip(1).Select(Global.Deserialize).ToArray();
            remaining = remaining.Skip(1);
        }

        // Deserialize data
        var data = new Data[0];

        if (remaining.Count() > 0
            && remaining.First() is SExpression.List dataNode
            && dataNode.Count() >= 1
            && dataNode[0] is SExpression.UnquotedSymbol dataNodeStartSymbol
            && dataNodeStartSymbol.Value == "data")
        {
            data = dataNode.Skip(1).Select(Bytecode.Data.Deserialize).ToArray();
            remaining = remaining.Skip(1);
        }

        var module = new Module(name, globals, data);

        foreach (var item in remaining)
            module.AddFunction(Function.Deserialize(item));

        return module;
    }

}