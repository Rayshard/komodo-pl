using Komodo.Core.Utilities;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

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

    public ReadOnlyCollection<OptionallyNamedDataType> Parameters { get; }
    public ReadOnlyDictionary<string, NamedDataType> NamedParameters { get; }

    public ReadOnlyCollection<DataType> Returns { get; }

    public ReadOnlyCollection<OptionallyNamedDataType> Locals { get; }
    public ReadOnlyDictionary<string, NamedDataType> NamedLocals { get; }

    private List<FunctionBodyElement> bodyElements = new List<FunctionBodyElement>();
    public ReadOnlyCollection<FunctionBodyElement> BodyElements => new ReadOnlyCollection<FunctionBodyElement>(bodyElements);

    public ReadOnlyDictionary<string, Label> Labels { get; private set; } = new ReadOnlyDictionary<string, Label>(new Dictionary<string, Label>());
    public ReadOnlyCollection<Instruction> Instructions { get; private set; } = new ReadOnlyCollection<Instruction>(new Instruction[0]);

    public Function(string name, IEnumerable<OptionallyNamedDataType> parameters, IEnumerable<DataType> returns, IEnumerable<OptionallyNamedDataType> locals)
    {
        Name = name;
        Parameters = new ReadOnlyCollection<OptionallyNamedDataType>(parameters.ToArray());
        Returns = new ReadOnlyCollection<DataType>(returns.ToArray());
        Locals = new ReadOnlyCollection<OptionallyNamedDataType>(locals.ToArray());
        NamedParameters = new ReadOnlyDictionary<string, NamedDataType>(
            Parameters.Where(p => p.Name is not null).Select(p => p.ToNamed()).AssertAllDistinct(p => p.Name).ToDictionary(p => p.Name)
        );
        NamedLocals = new ReadOnlyDictionary<string, NamedDataType>(
            Locals.Where(l => l.Name is not null).Select(l => l.ToNamed()).AssertAllDistinct(l => l.Name).ToDictionary(l => l.Name)
        );
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

        var paramsNodes = new List<SExpression>();
        paramsNodes.Add(new SExpression.UnquotedSymbol("params"));
        paramsNodes.AddRange(Parameters.Select(param => new SExpression.UnquotedSymbol(param.ToString())));
        nodes.Add(new SExpression.List(paramsNodes));

        var returnsNodes = new List<SExpression>();
        returnsNodes.Add(new SExpression.UnquotedSymbol("returns"));
        returnsNodes.AddRange(Returns.Select(local => new SExpression.UnquotedSymbol(local.ToString())));
        nodes.Add(new SExpression.List(returnsNodes));

        if (Locals.Count != 0)
        {
            var localsNodes = new List<SExpression>();

            localsNodes.Add(new SExpression.UnquotedSymbol("locals"));
            localsNodes.AddRange(Locals.Select(local => new SExpression.UnquotedSymbol(local.ToString())));
            nodes.Add(new SExpression.List(localsNodes));
        }

        nodes.AddRange(bodyElements.Select(be => be.AsSExpression()));

        return new SExpression.List(nodes);
    }

    public static Function Deserialize(SExpression sexpr)
    {
        var remaining = sexpr.ExpectList().ExpectLength(3, null).AsEnumerable();

        remaining.First().ExpectUnquotedSymbol().ExpectValue("function");
        remaining = remaining.Skip(1);

        var name = remaining.First().ExpectUnquotedSymbol().Value;
        remaining = remaining.Skip(1);

        // Deserialize parameters
        var parameters = new OptionallyNamedDataType[0];

        if (remaining.Count() > 0
            && remaining.First() is SExpression.List parametersNode
            && parametersNode.Count() >= 1
            && parametersNode[0] is SExpression.UnquotedSymbol parametersNodeStartSymbol
            && parametersNodeStartSymbol.Value == "params")
        {
            parameters = parametersNode.Skip(1).Select(item => OptionallyNamedDataType.Deserialize(item)).ToArray();
            remaining = remaining.Skip(1);
        }

        // Deserialize returns
        var returns = new DataType[0];

        if (remaining.Count() > 0
            && remaining.First() is SExpression.List returnsNode
            && returnsNode.Count() >= 1
            && returnsNode[0] is SExpression.UnquotedSymbol returnsNodeStartSymbol
            && returnsNodeStartSymbol.Value == "returns")
        {
            returns = returnsNode.Skip(1).Select(DataType.Deserialize).ToArray();
            remaining = remaining.Skip(1);
        }

        // Deserialize locals
        var locals = new OptionallyNamedDataType[0];

        if (remaining.Count() > 0
            && remaining.First() is SExpression.List localsNode
            && localsNode.Count() >= 1
            && localsNode[0] is SExpression.UnquotedSymbol localsNodeStartSymbol
            && localsNodeStartSymbol.Value == "locals")
        {
            locals = localsNode.Skip(1).Select(item => OptionallyNamedDataType.Deserialize(item)).ToArray();
            remaining = remaining.Skip(1);
        }

        var function = new Function(name, parameters, returns, locals);

        foreach (var item in remaining)
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