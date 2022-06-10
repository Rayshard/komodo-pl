namespace Komodo.Compilation.ConcreteSyntaxTree;

using Komodo.Utilities;

public enum CSTNodeType
{
    Module,
    Symbol,
    Literal,
    BinopExpression,
    BinaryOperator,
    ParenthesizedExpression,
}

public interface ICSTNode
{
    public CSTNodeType NodeType { get; }
    public Location Location { get; }
    public ICSTNode[] Children { get; }
}

public record CSTAtom(CSTNodeType NodeType, Token Token) : ICSTNode
{
    public Location Location => Token.Location;
    public ICSTNode[] Children => new ICSTNode[] { };
}

public record CSTModule(Token LBracket, ICSTNode[] Children, Token RBracket) : ICSTNode
{
    public CSTNodeType NodeType => CSTNodeType.Module;
    public Location Location => new Location(LBracket.Location.SourceFileName, new Span(LBracket.Location.Span.Start, RBracket.Location.Span.End));
}

public interface ICSTExpression : ICSTNode { }

public enum LiteralType { Int, String, Bool, Char }

public record CSTLiteral(Token Token) : CSTAtom(CSTNodeType.Literal, Token), ICSTExpression
{
    public LiteralType LiteralType => Token.Type switch
    {
        TokenType.IntLit => LiteralType.Int,
        var type => throw new NotImplementedException(type.ToString()),
    };
}

public enum BinaryOperation { Add, Sub, Multiply, Divide };
public enum BinaryOperationAssociativity { Left, Right, None };

public record CSTBinaryOperator(Token Token) : CSTAtom(CSTNodeType.BinaryOperator, Token)
{
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

public record CSTBinopExpression(ICSTExpression Left, CSTBinaryOperator Op, ICSTExpression Right) : ICSTExpression
{
    public CSTNodeType NodeType => CSTNodeType.BinopExpression;
    public Location Location => new Location(Op.Location.SourceFileName, new Span(Left.Location.Span.Start, Right.Location.Span.End));
    public ICSTNode[] Children => new ICSTNode[] { Left, Op, Right };
}

public record CSTParenthesizedExpression(Token LParen, ICSTExpression Expression, Token RParen) : ICSTExpression
{
    public CSTNodeType NodeType => CSTNodeType.ParenthesizedExpression;
    public Location Location => new Location(Expression.Location.SourceFileName, new Span(LParen.Location.Span.Start, RParen.Location.Span.End));
    public ICSTNode[] Children => new ICSTNode[] { new CSTAtom(CSTNodeType.Symbol, LParen), Expression, new CSTAtom(CSTNodeType.Symbol, RParen) };
}

public static class Extensions
{
    public static bool IsExpression(this ICSTNode node) => node is ICSTExpression;

    public static bool IsExpression(this CSTNodeType nodeType) => nodeType switch
    {
        CSTNodeType.Literal => true,
        CSTNodeType.BinopExpression => true,
        CSTNodeType.ParenthesizedExpression => true,
        _ => false
    };
}