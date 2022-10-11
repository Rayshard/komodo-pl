using Komodo.Core.Utilities;

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

public record Function(
    string Name,
    VSROCollection<OptionallyNamedDataType> Parameters,
    VSROCollection<DataType> Returns,
    VSROCollection<OptionallyNamedDataType> Locals,
    VSROCollection<Instruction> Instructions,
    VSRODictionary<string, Label> Labels
)
{
    public Dictionary<string, NamedDataType> NamedParameters => Parameters.Where(p => p.Name is not null).Select(p => p.ToNamed()).ToDictionary(p => p.Name);
    public Dictionary<string, NamedDataType> NamedLocals => Locals.Where(l => l.Name is not null).Select(l => l.ToNamed()).ToDictionary(l => l.Name);

    public IEnumerable<FunctionBodyElement> BodyElements
    {
        get
        {
            var bodyElements = new List<FunctionBodyElement>();
            var instrIndexToLabelMap = Labels.Values.ToDictionary(l => l.Target);

            foreach (var (i, instruction) in Instructions.WithIndices())
            {
                if (instrIndexToLabelMap.TryGetValue((UInt64)i, out var label))
                    bodyElements.Add(label);

                bodyElements.Add(instruction);
            }

            return bodyElements;
        }
    }

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("function"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));

        var paramsNodes = new List<SExpression>();
        paramsNodes.Add(new SExpression.UnquotedSymbol("params"));
        paramsNodes.AddRange(Parameters.Select(param => param.AsSExpression()));
        nodes.Add(new SExpression.List(paramsNodes));

        var returnsNodes = new List<SExpression>();
        returnsNodes.Add(new SExpression.UnquotedSymbol("returns"));
        returnsNodes.AddRange(Returns.Select(ret => ret.AsSExpression()));
        nodes.Add(new SExpression.List(returnsNodes));

        if (Locals.Count != 0)
        {
            var localsNodes = new List<SExpression>();
            localsNodes.Add(new SExpression.UnquotedSymbol("locals"));
            localsNodes.AddRange(Locals.Select(local => local.AsSExpression()));
            nodes.Add(new SExpression.List(localsNodes));
        }

        nodes.AddRange(BodyElements.Select(be => be.AsSExpression()));
        return new SExpression.List(nodes);
    }

    public static Function Deserialize(SExpression sexpr) => new FunctionBuilder(sexpr).Build();
}

public class FunctionBuilder
{
    private string? name;

    private List<OptionallyNamedDataType> parameters = new List<OptionallyNamedDataType>();
    private Dictionary<string, int> namedParameters = new Dictionary<string, int>();

    private List<OptionallyNamedDataType> locals = new List<OptionallyNamedDataType>();
    private Dictionary<string, int> namedLocals = new Dictionary<string, int>();

    private List<DataType> returns = new List<DataType>();

    private List<Instruction> instructions = new List<Instruction>();
    private Dictionary<string, Label> labels = new Dictionary<string, Label>();

    public FunctionBuilder(SExpression sexpr)
    {
        var remaining = sexpr.ExpectList()
                             .ExpectLength(4, null)
                             .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("function"))
                             .ExpectItem(1, item => SetName(item.ExpectUnquotedSymbol().Value))
                             .ExpectItem(2, item => item.ExpectList()
                                                        .ExpectLength(1, null)
                                                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("params")),
                                            out var paramsList)
                             .ExpectItem(3, item => item.ExpectList()
                                                        .ExpectLength(1, null)
                                                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("returns")),
                                            out var returnsList)
                             .Skip(4);

        paramsList.ExpectItems(item => AddParameter(OptionallyNamedDataType.Deserialize(item)), 1);
        returnsList.ExpectItems(item => AddReturn(DataType.Deserialize(item)), 1);

        // Deserialize locals
        if (remaining.Count() > 0
            && remaining.First() is SExpression.List localsNode
            && localsNode.Count() >= 1
            && localsNode[0] is SExpression.UnquotedSymbol localsNodeStartSymbol
            && localsNodeStartSymbol.Value == "locals")
        {
            foreach (var local in localsNode.Skip(1).Select(item => OptionallyNamedDataType.Deserialize(item)))
                AddLocal(local);

            remaining = remaining.Skip(1);
        }

        foreach (var item in remaining)
        {
            // This is an interesting try/catch block because we only want to ignore
            // the deserialization if it fails with a FormatException, but not
            // ignore if deserialization 'went well', but we were unable to append it 
            // to the function. Otherwise we deserialize the item as an instruction.
            try { AppendLabel(Label.Deserialize(item)); }
            catch (SExpression.FormatException) { AppendInstruction(Instruction.Deserialize(item)); }
        }
    }

    public void SetName(string? value) => name = value;

    public void AddParameter(OptionallyNamedDataType parameter)
    {
        if (parameter.Name is not null)
            namedParameters.Add(parameter.Name, parameters.Count);

        parameters.Add(parameter);
    }

    public bool HasParameter(string name) => namedParameters.ContainsKey(name);
    public OptionallyNamedDataType GetParameter(int index) => parameters[index];
    public NamedDataType GetParameter(string name) => parameters[namedParameters[name]].ToNamed();

    public void AddLocal(OptionallyNamedDataType local)
    {
        if (local.Name is not null)
            namedLocals.Add(local.Name, locals.Count);

        locals.Add(local);
    }

    public bool HasLocal(string name) => namedLocals.ContainsKey(name);
    public OptionallyNamedDataType GetLocal(int index) => locals[index];
    public NamedDataType GetLocal(string name) => locals[namedLocals[name]].ToNamed();

    public void AddReturn(DataType dataType) => returns.Add(dataType);
    public DataType GetReturn(int index) => returns[index];

    public void AppendLabel(string name) => labels.Add(name, new Label(name, (UInt64)instructions.Count));
    public void AppendInstruction(Instruction instruction) => instructions.Add(instruction);

    public Function Build()
    {
        var name = this.name ?? throw new Exception("Name is not set");
        var parameters = this.parameters.ToVSROCollection();
        var locals = this.locals.ToVSROCollection();
        var returns = this.returns.ToVSROCollection();
        var instructions = this.instructions.ToVSROCollection();
        var labels = this.labels.ToVSRODictionary();

        return new Function(name, parameters, returns, locals, instructions, labels);
    }
}