using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public class Module
{
    public string Name { get; }

    private Dictionary<string, Function> functions = new Dictionary<string, Function>();
    public IEnumerable<Function> Functions => functions.Values;

    public Module(string name) => Name = name;

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
        var list = sexpr.ExpectList().ExpectLength(2, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("module");

        var name = list[1].ExpectUnquotedSymbol().Value;
        var module = new Module(name);

        foreach (var item in list.Skip(2))
            module.AddFunction(Function.Deserialize(item));

        return module;
    }

}