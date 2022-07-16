using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public record Identifier : INode
{
    public string Name { get; }
    public Symbol Symbol { get; }
    public TextLocation Location { get; }

    private Identifier(string name, TextLocation location, Symbol symbol)
    {
        Name = name;
        Location = location;
        Symbol = symbol;
    }

    public NodeType NodeType => NodeType.Identifier;
    public INode[] Children => new INode[] { };

    public record Expression(string Name, TextLocation Location, TypedSymbol TypedSymbol) : Identifier(Name, Location, TypedSymbol), IExpression
    {
        public TSType TSType => TypedSymbol.TSType;
    }

    public record Typename(string Name, TextLocation Location, TypeSystem.Typename TSTypename) : Identifier(Name, Location, TSTypename) { }
}