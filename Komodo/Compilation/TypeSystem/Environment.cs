using Komodo.Utilities;

namespace Komodo.Compilation.TypeSystem;

public interface Symbol
{
    public string Name { get; }
    public TextLocation DefinitionLocation { get; }
}

public record TypedSymbol(string Name, TSType TSType, TextLocation DefinitionLocation) : Symbol { }
public record Typename(string Name, TSType TSType, TextLocation DefinitionLocation) : Symbol { }

public class Environment
{
    public Environment? Parent { get; }

    public Dictionary<string, Symbol> Symbols { get; }

    public Environment(Environment? parent = null)
    {
        Parent = parent;
        Symbols = new Dictionary<string, Symbol>();
    }

    public bool AddSymbol(Symbol symbol, Diagnostics diagnostics)
    {
        if (Symbols.ContainsKey(symbol.Name))
        {
            diagnostics.Add(Error.TSSymbolAlreadyDefined(symbol.Name, Symbols[symbol.Name].DefinitionLocation, symbol.DefinitionLocation));
            return false;
        }

        Symbols.Add(symbol.Name, symbol);
        return true;
    }

    public Symbol? GetSymbol(string name, TextLocation location, Diagnostics diagnostics, bool checkParent)
    {
        Symbol? symbol;

        if (!Symbols.TryGetValue(name, out symbol))
        {
            if (checkParent && Parent is not null)
                return Parent.GetSymbol(name, location, diagnostics, checkParent);

            diagnostics.Add(Error.TSSymbolDoesNotExist(name, location));
        }

        return symbol;
    }

    public TypedSymbol? GetTypedSymbol(string name, TextLocation location, Diagnostics diagnostics, bool checkParent)
    {
        var symbol = GetSymbol(name, location, diagnostics, checkParent);

        if (symbol is null) { return null; }
        else if (symbol is TypedSymbol) { return symbol as TypedSymbol; }
        else
        {
            diagnostics.Add(Error.TSSymbolIsNotAVariable(name, symbol.DefinitionLocation, location));
            return null;
        }
    }

    public Dictionary<string, TypedSymbol> TypedSymbols => new Dictionary<string, TypedSymbol>(from item in Symbols
                                                                                               where item.Value is TypedSymbol
                                                                                               select new KeyValuePair<string, TypedSymbol>(item.Key, (TypedSymbol)item.Value));

    public override string ToString()
    {
        using var writer = new StringWriter();

        writer.WriteLine("========== Environment ==========");

        foreach (var (id, value) in Symbols)
            writer.WriteLine($" -> {id}: {value}");


        writer.WriteLine("============== End ==============");

        return writer.ToString();
    }
}