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

    public record Expression(string Name, TextLocation Location, Symbol Symbol) : Identifier(Name, Location, Symbol), IExpression
    {
        public TSType TSType => Symbol switch
        {
            Symbol.Variable var => var.TSType,
            Symbol.Function func => func.TSType,
            var symbol => throw new NotImplementedException($"Identifier expressions can not contain symbols of type {symbol.GetType()}") 
        };
    }

    public record Typename(string Name, TextLocation Location, Symbol.Typename TSTypename) : Identifier(Name, Location, TSTypename) { }
}