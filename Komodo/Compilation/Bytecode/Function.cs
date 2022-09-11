using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public class Function
{
    public const string ENTRY_NAME = "__entry__";

    public string Name { get; }
    public DataType ReturnType { get; }

    private Dictionary<string, BasicBlock> basicBlocks = new Dictionary<string, BasicBlock>();
    public IEnumerable<BasicBlock> BasicBlocks => basicBlocks.Values;

    private DataType[] arguments;
    public IEnumerable<DataType> Arguments => arguments;

    private DataType[] locals;
    public IEnumerable<DataType> Locals => locals;

    public Function(string name, IEnumerable<DataType> arguments, IEnumerable<DataType> locals, DataType returnType)
    {
        Name = name;
        ReturnType = returnType;

        this.arguments = arguments.ToArray();
        this.locals = locals.ToArray();
    }

    public void AddBasicBlock(BasicBlock basicBlock) => basicBlocks.Add(basicBlock.Name, basicBlock);
    public BasicBlock GetBasicBlock(string name) => basicBlocks[name];

    public DataType GetArgument(int idx) => arguments[idx];
    public DataType GetLocal(int idx) => locals[idx];

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("function"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));

        var argsNodes = new List<SExpression>();
        argsNodes.Add(new SExpression.UnquotedSymbol("args"));
        argsNodes.AddRange(Arguments.Select(arg => new SExpression.UnquotedSymbol(arg.ToString())));
        nodes.Add(new SExpression.List(argsNodes));

        var localsNodes = new List<SExpression>();
        localsNodes.Add(new SExpression.UnquotedSymbol("locals"));
        localsNodes.AddRange(Locals.Select(arg => new SExpression.UnquotedSymbol(arg.ToString())));
        nodes.Add(new SExpression.List(localsNodes));

        var retNode = new SExpression.List(new[]
        {
            new SExpression.UnquotedSymbol("ret"),
            new SExpression.UnquotedSymbol(ReturnType.ToString()),
        });
        nodes.Add(new SExpression.List(retNode));

        nodes.AddRange(BasicBlocks.Select(bb => bb.AsSExpression()));

        return new SExpression.List(nodes);
    }

    public static Function Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(6, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("function");

        var name = list[1].ExpectUnquotedSymbol().Value;
        var argsNode = list[2].ExpectList().ExpectLength(1, null);
        var localsNode = list[3].ExpectList().ExpectLength(1, null);
        var retNode = list[4].ExpectList().ExpectLength(2);

        argsNode[0].ExpectUnquotedSymbol().ExpectValue("args");
        localsNode[0].ExpectUnquotedSymbol().ExpectValue("locals");
        retNode[0].ExpectUnquotedSymbol().ExpectValue("ret");

        var args = argsNode.Skip(1).Select(node => node.AsEnum<DataType>());
        var locals = localsNode.Skip(1).Select(node => node.AsEnum<DataType>());
        var ret = retNode.ElementAt(1).AsEnum<DataType>();
        var function = new Function(name, args, locals, ret);

        foreach (var item in list.Skip(5))
            function.AddBasicBlock(BasicBlock.Deserialize(item));

        return function;
    }

}