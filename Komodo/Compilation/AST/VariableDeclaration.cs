using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public record VariableDeclaration(string Identifier, IExpression Expression, TextLocation Location) : IStatement
{
    public NodeType NodeType => NodeType.VariableDeclaration;
    public TSType TSType => Expression.TSType;
    public INode[] Children => new INode[] { Expression };
}