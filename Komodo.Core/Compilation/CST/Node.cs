namespace Komodo.Core.Compilation.CST;

using Komodo.Core.Utilities;

public enum NodeType
{
    Token,
    Literal,
    BinaryOperator,
    BinopExpression,
    ParenthesizedExpression,
    VariableDeclaration,
    Identifier,
}

public interface INode
{
    public NodeType NodeType { get; }
    public TextLocation Location { get; }
    public INode[] Children { get; }
}

public interface IExpression : INode { }

public interface IStatement : INode { }

public static class Extensions
{
    public static bool IsExpression(this INode node) => node is IExpression;

    public static bool IsExpression(this NodeType nodeType) => nodeType switch
    {
        NodeType.Literal => true,
        NodeType.BinopExpression => true,
        NodeType.ParenthesizedExpression => true,
        NodeType.Identifier => true,
        _ => false
    };

    public static bool IsStatement(this NodeType nodeType) => nodeType switch
    {
        NodeType.VariableDeclaration => true,
        _ => false
    };
}