using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public class Function
{
    public const string ENTRY_NAME = "__entry__";

    public string Name { get; }

    private Dictionary<string, BasicBlock> basicBlocks = new Dictionary<string, BasicBlock>();
    public IEnumerable<BasicBlock> BasicBlocks => basicBlocks.Values;

    private DataType[] arguments;
    public IEnumerable<DataType> Arguments => arguments;

    private DataType[] locals;
    public IEnumerable<DataType> Locals => locals;

    private DataType[] returns;
    public IEnumerable<DataType> Returns => returns;

    public Function(string name, IEnumerable<DataType> arguments, IEnumerable<DataType> locals, IEnumerable<DataType> returns)
    {
        Name = name;

        this.arguments = arguments.ToArray();
        this.locals = locals.ToArray();
        this.returns = returns.ToArray();
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
        localsNodes.AddRange(Locals.Select(local => new SExpression.UnquotedSymbol(local.ToString())));
        nodes.Add(new SExpression.List(localsNodes));

        var returnsNodes = new List<SExpression>();
        returnsNodes.Add(new SExpression.UnquotedSymbol("returns"));
        returnsNodes.AddRange(Returns.Select(local => new SExpression.UnquotedSymbol(local.ToString())));
        nodes.Add(new SExpression.List(returnsNodes));

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
        var returnsNode = list[4].ExpectList().ExpectLength(1, null);

        argsNode[0].ExpectUnquotedSymbol().ExpectValue("args");
        localsNode[0].ExpectUnquotedSymbol().ExpectValue("locals");
        returnsNode[0].ExpectUnquotedSymbol().ExpectValue("returns");

        var args = argsNode.Skip(1).Select(node => node.Expect(DataType.Deserialize));
        var locals = localsNode.Skip(1).Select(node => node.Expect(DataType.Deserialize));
        var returns = returnsNode.Skip(1).Select(node => node.Expect(DataType.Deserialize));

        var function = new Function(name, args, locals, returns);

        foreach (var item in list.Skip(5))
            function.AddBasicBlock(BasicBlock.Deserialize(item));

        return function;
    }

}