using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public record VariableDeclaration(IdentifierExpression Identifier, IExpression Expression, TextLocation Location) : IStatement
{
    public NodeType NodeType => NodeType.VariableDeclaration;
    public TSType TSType => Expression.TSType;
    public INode[] Children => new INode[] { Identifier, Expression };
}