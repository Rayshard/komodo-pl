namespace Komodo.Compilation.Syntax;

using Komodo.Utilities;

public enum SyntaxTreeType
{
    Module,
    BinaryOperator,
    IntegerLiteral,
    BinopExpression,
    ParenthesizedExpression,
}

public abstract record SyntaxTree(SyntaxTreeType Type, Location Location)
{
    public abstract IEnumerable<Token> Tokens { get; }
}

public abstract record Expression(SyntaxTreeType Type, Location Location)
    : SyntaxTree(Type, Location);

public record Module(Expression[] Expressions, Location Location)
    : SyntaxTree(SyntaxTreeType.Module, Location)
{
    public override IEnumerable<Token> Tokens => Expressions.Aggregate(new Token[] {}, (acc, expr) => acc.Concat(expr.Tokens).ToArray());
}

public record IntegerLiteral(Token Token)
    : Expression(SyntaxTreeType.IntegerLiteral, Token.Location)
{
    public override IEnumerable<Token> Tokens => new[] { Token };
}

public enum BinaryOperatorKind { ADD, SUB, MULTIPLY, DIVIDE };
public record BinaryOperator(Token Token)
    : SyntaxTree(SyntaxTreeType.BinaryOperator, Token.Location)
{
    public override IEnumerable<Token> Tokens => new[] { Token };

    public BinaryOperatorKind Kind => Token.Type switch
    {
        TokenType.Plus => BinaryOperatorKind.ADD,
        TokenType.Minus => BinaryOperatorKind.SUB,
        TokenType.Asterisk => BinaryOperatorKind.MULTIPLY,
        TokenType.ForwardSlash => BinaryOperatorKind.DIVIDE,
        var tt => throw new Exception($"Unexpected token type: {tt}")
    };
};

public record BinopExpression(Expression Left, BinaryOperator Op, Expression Right)
    : Expression(SyntaxTreeType.BinopExpression, new Location(Op.Location.SourceFile, new Span(Left.Location.Span.Start, Right.Location.Span.End)))
{
    public override IEnumerable<Token> Tokens => Left.Tokens.Append(Op.Token).Concat(Right.Tokens);
}

public record ParenthesizedExpression(Token LParen, Expression InnerExpression, Token RParen)
    : Expression(SyntaxTreeType.ParenthesizedExpression, new Location(InnerExpression.Location.SourceFile, new Span(LParen.Location.Span.Start, RParen.Location.Span.End)))
{
    public override IEnumerable<Token> Tokens => InnerExpression.Tokens.Prepend(LParen).Append(RParen);
}
