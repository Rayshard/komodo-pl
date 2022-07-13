using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public enum NodeType
{
    IntLiteral,
    BoolLiteral,

    BinopExpression,
    IdentifierExpression,
    VariableDeclaration,
}

public interface INode
{
    public NodeType NodeType { get; }
    public TextLocation Location { get; }
    public TSType TSType { get; }
    public INode[] Children { get; }
}

public interface IExpression : INode { }

public interface IStatement : INode { }

public static class Extensions
{
    public static bool IsExpression(this INode node) => node is IExpression;

    public static bool IsExpression(this NodeType nodeType) => nodeType switch
    {
        NodeType.IntLiteral or
        NodeType.BoolLiteral or
        NodeType.BinopExpression or
        NodeType.IdentifierExpression => true,
        _ => false
    };

    public static bool IsStatement(this INode node) => node is IStatement;

    public static bool IsStatement(this NodeType nodeType) => nodeType switch
    {
        NodeType.VariableDeclaration => true,
        _ => false
    };
}