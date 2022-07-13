using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public record IdentifierExpression(string Value, TextLocation Location, TSType TSType) : IExpression
{
    public NodeType NodeType => NodeType.IdentifierExpression;
    public INode[] Children => new INode[] { };
}