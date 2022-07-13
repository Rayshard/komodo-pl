using Komodo.Utilities;

namespace Komodo.Compilation.TypeSystem;

public record Symbol(string Name, TextLocation DefinitionLocation);
public record Variable(string Name, TSType TSType, TextLocation DefinitionLocation) : Symbol(Name, DefinitionLocation);

public class Environment
{
    public Environment? Parent => null;

    private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();

    public void AddSymbol(Symbol symbol, Diagnostics diagnostics)
    {
        if (symbols.ContainsKey(symbol.Name))
        {
            diagnostics.Add(Error.TSSymbolAlreadyDefined(symbol.Name, symbols[symbol.Name].DefinitionLocation, symbol.DefinitionLocation));
            return;
        }

        symbols.Add(symbol.Name, symbol);
    }

    public Symbol? GetSymbol(string name, TextLocation location, Diagnostics diagnostics, bool checkParent)
    {
        Symbol? symbol;

        if (!symbols.TryGetValue(name, out symbol))
        {
            if (checkParent && Parent is not null)
                return Parent.GetSymbol(name, location, diagnostics, checkParent);

            diagnostics.Add(Error.TSSymbolDoesNotExist(name, location));
        }

        return symbol;
    }

    public Variable? GetVaraible(string name, TextLocation location, Diagnostics diagnostics, bool checkParent)
    {
        var symbol = GetSymbol(name, location, diagnostics, checkParent);

        if (symbol is null) { return null; }
        else if (symbol is Variable) { return symbol as Variable; }
        else
        {
            diagnostics.Add(Error.TSSymbolIsNotAVariable(name, symbol.DefinitionLocation, location));
            return null;
        }
    }

    public Dictionary<string, Variable> Variables => new Dictionary<string, Variable>(from item in symbols
                                                                                      where item.Value is Variable
                                                                                      select new KeyValuePair<string, Variable>(item.Key, (Variable)item.Value));
}