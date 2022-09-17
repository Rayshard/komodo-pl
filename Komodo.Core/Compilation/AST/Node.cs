using Komodo.Core.Utilities;
using Komodo.Core.Compilation.TypeSystem;

namespace Komodo.Core.Compilation.AST;

public enum NodeType
{
    IntLiteral,
    BoolLiteral,

    Identifier,
    BinopExpression,
    VariableDeclaration,
}

public interface INode
{
    public NodeType NodeType { get; }
    public TextLocation Location { get; }
    public INode[] Children { get; }
}

public interface IExpression : INode 
{
    public TSType TSType { get; }
}

public interface IStatement : INode { }

public static class Extensions
{
    public static bool IsExpression(this INode node) => node is IExpression;

    public static bool IsExpression(this NodeType nodeType) => nodeType switch
    {
        NodeType.IntLiteral or
        NodeType.BoolLiteral or
        NodeType.BinopExpression or
        NodeType.Identifier => true,
        _ => false
    };

    public static bool IsStatement(this INode node) => node is IStatement;

    public static bool IsStatement(this NodeType nodeType) => nodeType switch
    {
        NodeType.VariableDeclaration => true,
        _ => false
    };
}