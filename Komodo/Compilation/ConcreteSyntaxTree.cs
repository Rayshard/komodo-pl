namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public enum NodeType
{
    Token,
    Symbol,
    Literal,
    BinopExpression,
    BinaryOperator,
    ParenthesizedExpression,
}

public interface INode
{
    public NodeType NodeType { get; }
    public TextLocation Location { get; }
    public INode[] Children { get; }
}

public record Module(TextSource Source, INode[] Nodes);

public enum TokenType
{
    Invalid,
    IntLit,
    Plus,
    Minus,
    Asterisk,
    ForwardSlash,
    LParen,
    RParen,
    LCBracket,
    RCBracket,
    EOF,
}

public record Token(TokenType Type, TextLocation Location, string Value) : INode
{
    public NodeType NodeType => NodeType.Token;
    public INode[] Children => new INode[] { };
    public override string ToString() => $"{Type.ToString()}({Value}) at {Location}";
}

public interface IExpression : INode { }

public enum LiteralType { Int, String, Bool, Char }

public record Literal(Token Token) : IExpression
{
    public NodeType NodeType => NodeType.Literal;
    public TextLocation Location => Token.Location;
    public INode[] Children => new INode[] { Token };

    public LiteralType LiteralType => Token.Type switch
    {
        TokenType.IntLit => LiteralType.Int,
        var type => throw new NotImplementedException(type.ToString()),
    };
}

public enum BinaryOperation { Add, Sub, Multiply, Divide };
public enum BinaryOperationAssociativity { Left, Right, None };

public record BinaryOperator(Token Token) : INode
{
    public NodeType NodeType => NodeType.BinaryOperator;
    public TextLocation Location => Token.Location;
    public INode[] Children => new INode[] { Token };

    public BinaryOperation Operation => Token.Type switch
    {
        TokenType.Plus => BinaryOperation.Add,
        TokenType.Minus => BinaryOperation.Sub,
        TokenType.Asterisk => BinaryOperation.Multiply,
        TokenType.ForwardSlash => BinaryOperation.Divide,
        var type => throw new NotImplementedException(type.ToString()),
    };

    public BinaryOperationAssociativity Asssociativity => Operation switch
    {
        BinaryOperation.Add => BinaryOperationAssociativity.Left,
        BinaryOperation.Sub => BinaryOperationAssociativity.Left,
        BinaryOperation.Multiply => BinaryOperationAssociativity.Left,
        BinaryOperation.Divide => BinaryOperationAssociativity.Left,
        var type => throw new NotImplementedException(type.ToString()),
    };

    public int Precedence => Operation switch
    {
        BinaryOperation.Add => 0,
        BinaryOperation.Sub => 0,
        BinaryOperation.Multiply => 1,
        BinaryOperation.Divide => 1,
        var op => throw new NotImplementedException(op.ToString()),
    };
}

public record BinopExpression(IExpression Left, BinaryOperator Op, IExpression Right) : IExpression
{
    public NodeType NodeType => NodeType.BinopExpression;
    public TextLocation Location => new TextLocation(Op.Location.SourceName, Left.Location.Start, Right.Location.End);
    public INode[] Children => new INode[] { Left, Op, Right };
}

public record ParenthesizedExpression(Token LParen, IExpression Expression, Token RParen) : IExpression
{
    public NodeType NodeType => NodeType.ParenthesizedExpression;
    public TextLocation Location => new TextLocation(Expression.Location.SourceName, LParen.Location.Start, RParen.Location.End);
    public INode[] Children => new INode[] { LParen, Expression, RParen };
}

public static class Extensions
{
    public static bool IsExpression(this INode node) => node is IExpression;

    public static bool IsExpression(this NodeType nodeType) => nodeType switch
    {
        NodeType.Literal => true,
        NodeType.BinopExpression => true,
        NodeType.ParenthesizedExpression => true,
        _ => false
    };
}