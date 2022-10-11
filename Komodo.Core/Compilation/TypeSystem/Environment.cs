using System.Collections.ObjectModel;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.TypeSystem;

public class Environment
{
    public record OperatorOverload(TSOperator Operator, TextLocation DefinitionLocation);

    private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();
    private Dictionary<OperatorKind, List<OperatorOverload>> operators = new Dictionary<OperatorKind, List<OperatorOverload>>();

    public Environment? Parent { get; }

    public Environment(Environment? parent = null)
    {
        Parent = parent;

        foreach (OperatorKind kind in Enum.GetValues(typeof(OperatorKind)))
            operators[kind] = new List<OperatorOverload>();
    }

    public bool AddSymbol(Symbol symbol, Diagnostics? diagnostics)
    {
        if (symbols.ContainsKey(symbol.Name))
        {
            diagnostics?.Add(Error.TSSymbolAlreadyDefined(symbol.Name, symbols[symbol.Name].DefinitionLocation, symbol.DefinitionLocation));
            return false;
        }

        symbols.Add(symbol.Name, symbol);
        return true;
    }

    public Symbol? GetSymbol(string name, TextLocation location, Diagnostics? diagnostics, bool checkParent)
    {
        Symbol? symbol;

        if (!symbols.TryGetValue(name, out symbol))
        {
            if (checkParent && Parent is not null)
                return Parent.GetSymbol(name, location, diagnostics, checkParent);

            diagnostics?.Add(Error.TSSymbolDoesNotExist(name, location));
        }

        return symbol;
    }

    public Symbol.Variable? GetVariable(string name, TextLocation location, Diagnostics? diagnostics, bool checkParent)
    {
        var symbol = GetSymbol(name, location, diagnostics, checkParent);

        if (symbol is null) { return null; }
        else if (symbol is Symbol.Variable) { return symbol as Symbol.Variable; }
        else
        {
            diagnostics?.Add(Error.TSSymbolIsNotAVariable(name, symbol.DefinitionLocation, location));
            return null;
        }
    }

    public Dictionary<string, T> GetAll<T>() where T : Symbol => new Dictionary<string, T>(from item in symbols
                                                                                           where item.Value is T
                                                                                           select new KeyValuePair<string, T>(item.Key, (T)item.Value));

    public bool AddOperatorOverload(OperatorOverload overload, TextLocation location, Diagnostics? diagnostics)
    {
        var exisitingOverload = GetOperatorOverload(overload.Operator.Kind, overload.Operator.Parameters, location, null, false);
        if (exisitingOverload is not null)
        {
            diagnostics?.Add(Error.OperatorOverloadWithParametersAlreadyExists(exisitingOverload, location));
            return false;
        }

        operators[overload.Operator.Kind].Add(overload);
        return true;
    }

    public OperatorOverload? GetOperatorOverload(OperatorKind op, ReadOnlyCollection<TSType> args, TextLocation location, Diagnostics? diagnostics, bool checkParent)
    {
        foreach (var overload in operators[op])
        {
            if (overload.Operator.Accepts(args))
                return overload;
        }

        if (checkParent && Parent is not null)
            return Parent.GetOperatorOverload(op, args, location, diagnostics, checkParent);

        diagnostics?.Add(Error.OperatorOverloadDoesNotExist(op, args, location));
        return null;
    }

    public override string ToString()
    {
        using var writer = new StringWriter();

        writer.WriteLine("========== Environment ==========");

        foreach (var (_, value) in symbols)
            writer.WriteLine(value);

        foreach (var (kind, overloads) in operators)
            foreach (var overload in overloads)
                writer.WriteLine(overload);

        writer.WriteLine("============== End ==============");

        return writer.ToString();
    }
}