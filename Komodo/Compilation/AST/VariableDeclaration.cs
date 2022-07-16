using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public record VariableDeclaration(TypedSymbol Symbol, IExpression Expression, TextLocation Location) : IStatement
{
    public NodeType NodeType => NodeType.VariableDeclaration;
    public INode[] Children => new INode[] { Expression };
}