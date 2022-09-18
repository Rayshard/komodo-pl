using Komodo.Core.Utilities;
using System.Collections.ObjectModel;

namespace Komodo.Core.Compilation.Bytecode;

public interface FunctionBodyElement
{
    SExpression AsSExpression();
}

public record Label(string Name, UInt64 Target) : FunctionBodyElement
{
    public SExpression AsSExpression() => new SExpression.List(new[]{
        new SExpression.UnquotedSymbol("label"),
        new SExpression.UnquotedSymbol(Name),
    });

    public static string Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(2);
        list[0].ExpectUnquotedSymbol().ExpectValue("label");

        return list[1].ExpectUnquotedSymbol().Value;
    }
}

public class Function
{
    public string Name { get; }

    private DataType[] arguments;
    public ReadOnlyCollection<DataType> Arguments => new ReadOnlyCollection<DataType>(arguments);

    private DataType[] locals;
    public ReadOnlyCollection<DataType> Locals => new ReadOnlyCollection<DataType>(locals);

    private DataType[] returns;
    public ReadOnlyCollection<DataType> Returns => new ReadOnlyCollection<DataType>(returns);

    private List<FunctionBodyElement> bodyElements = new List<FunctionBodyElement>();
    public ReadOnlyCollection<FunctionBodyElement> BodyElements => new ReadOnlyCollection<FunctionBodyElement>(bodyElements);

    public ReadOnlyDictionary<string, Label> Labels { get; private set; } = new ReadOnlyDictionary<string, Label>(new Dictionary<string, Label>());
    public ReadOnlyCollection<Instruction> Instructions { get; private set; } = new ReadOnlyCollection<Instruction>(new Instruction[0]);

    public Function(string name, IEnumerable<DataType> arguments, IEnumerable<DataType> locals, IEnumerable<DataType> returns)
    {
        Name = name;

        this.arguments = arguments.ToArray();
        this.locals = locals.ToArray();
        this.returns = returns.ToArray();
    }

    public void AppendLabel(string name)
    {
        if (Labels.ContainsKey(name))
            throw new Exception($"Label with name '{name}' already exists for function '{Name}'!");

        bodyElements.Add(new Label(name, (UInt64)Instructions.Count));
        Labels = new ReadOnlyDictionary<string, Label>(bodyElements.Where(be => be is Label).Cast<Label>().ToDictionary(label => label.Name));
    }

    public void AppendInstruction(Instruction instruction)
    {
        bodyElements.Add(instruction);
        Instructions = new ReadOnlyCollection<Instruction>(bodyElements.Where(be => be is Instruction).Cast<Instruction>().ToArray());
    }

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

        nodes.AddRange(bodyElements.Select(be => be.AsSExpression()));

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
        {
            // This is an interesting try/catch block because we only want to ignore
            // the deserialization if it fails with a FormatException, but not
            // ignore if deserialization 'went well', but we were unable to append it 
            // to the function. Otherwise we deserialize the item as an instruction.
            try { function.AppendLabel(Label.Deserialize(item)); }
            catch (SExpression.FormatException) { function.AppendInstruction(Instruction.Deserialize(item)); }
        }

        return function;
    }

}