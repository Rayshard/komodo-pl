namespace Komodo.Compilation.Syntax;

using Komodo.Utilities;

public enum SyntaxType
{
    BinaryOperator,
    IntegerLiteral,
    BinopExpression,
    ParenthesizedExpression,
}

public abstract record Syntax(SyntaxType Type, Location Location, Token[] Tokens);
public abstract record Expression(SyntaxType Type, Location Location, Token[] Tokens) : Syntax(Type, Location, Tokens);

public record IntegerLiteral(Token Token) : Expression(SyntaxType.IntegerLiteral, Token.Location, new Token[] { Token });

public enum BinaryOperatorKind { ADD, SUB, MULTIPLY, DIVIDE };
public record BinaryOperator(Token Token)
    : Syntax(SyntaxType.BinaryOperator, Token.Location, new Token[] { Token })
{
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
    : Expression(SyntaxType.BinopExpression, new Location(Op.Location.SourceFile, new Span(Left.Location.Span.Start, Right.Location.Span.End)), Left.Tokens.Append(Op.Token).Concat(Right.Tokens).ToArray());

public record ParenthesizedExpression(Token LParen, Expression InnerExpression, Token RParen)
    : Expression(SyntaxType.ParenthesizedExpression, new Location(InnerExpression.Location.SourceFile, new Span(LParen.Location.Span.Start, RParen.Location.Span.End)), InnerExpression.Tokens.Prepend(LParen).Append(RParen).ToArray());
