namespace Komodo.Compilation;

using Komodo.Compilation.CST;
using Komodo.Utilities;
using System.Collections.ObjectModel;

public static class ParseError
{
    public static Diagnostic ExpectedToken(TokenType expected, Token found)
    {
        var message = $"Expected {expected} but found {found.Type}({found.Value})";
        var lineHints = new LineHint[] { new LineHint(found.Location, $"expected {expected}") };

        return new Diagnostic(DiagnosticType.Error, found.Location, message, lineHints);
    }

    public static Diagnostic UnexpectedToken(Token token)
    {
        var message = $"Encountered unexpected token: {token.Type}({token.Value})";
        var lineHints = new LineHint[] { new LineHint(token.Location, "unexpected token") };

        return new Diagnostic(DiagnosticType.Error, token.Location, message, lineHints);
    }
}

public static class Parser
{
    static ReadOnlyCollection<TokenType> BinaryOperatorTokens = new ReadOnlyCollection<TokenType>(new[]
    {
        TokenType.Plus, TokenType.Minus, TokenType.Asterisk, TokenType.ForwardSlash
    });


    public static (T?, Diagnostics) Try<T>(Func<TokenStream, Diagnostics?, T?> parseFunc, TokenStream stream)
    {
        var streamStart = stream.Offset;
        var diagnostics = new Diagnostics();
        var node = parseFunc(stream, diagnostics);

        if (node == null)
            stream.Offset = streamStart;

        return (node, diagnostics);
    }

    public static Token? ExpectToken(TokenType type, TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        if (token.Type != type)
        {
            diagnostics?.Add(ParseError.ExpectedToken(type, token));
            return null;
        }

        return token;
    }

    public static BinaryOperator? ParseBinop(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        if (!BinaryOperatorTokens.Contains(token.Type))
        {
            diagnostics?.Add(ParseError.UnexpectedToken(token));
            return null;
        }

        return new BinaryOperator(token);
    }

    public static Literal? ParseLiteral(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.IntLit: return new Literal(token);
            default:
                diagnostics?.Add(ParseError.UnexpectedToken(token));
                return null;
        }
    }

    public static ParenthesizedExpression? ParseParenthesizedExpression(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var lParen = ExpectToken(TokenType.LParen, stream, diagnostics);
        if (lParen == null)
            return null;

        var expr = ParseExpression(stream, diagnostics);
        if (expr == null)
            return null;

        var rParen = ExpectToken(TokenType.RParen, stream, diagnostics);
        if (rParen == null)
            return null;

        return new ParenthesizedExpression(lParen, expr, rParen);
    }

    public static IExpression? ParseAtom(TokenStream stream, Diagnostics? diagnostics = null)
    {
        IExpression? atom = null;

        (atom, var parenthesizedExpressionDiagnostics) = Try(ParseParenthesizedExpression, stream);
        if (atom != null)
        {
            diagnostics?.Append(parenthesizedExpressionDiagnostics);
            return atom;
        }

        (atom, var literalDiagnostics) = Try(ParseLiteral, stream);
        if (atom != null)
        {
            diagnostics?.Append(literalDiagnostics);
            return atom;
        }

        diagnostics?.Add(ParseError.UnexpectedToken(stream.Next()));
        return atom;
    }

    public static IExpression? ParseExpressionAtPrecedence(int minPrecedence, TokenStream stream, Diagnostics? diagnostics = null)
    {
        var expr = ParseAtom(stream, diagnostics);

        if (expr != null)
        {
            while (true)
            {
                var streamStart = stream.Offset;

                var (binop, _) = Try(ParseBinop, stream);
                if (binop == null || binop.Precedence < minPrecedence)
                {
                    stream.Offset = streamStart;
                    break;
                }

                var nextMinPrecedence = binop.Asssociativity == BinaryOperationAssociativity.Right ? binop.Precedence : (binop.Precedence + 1);
                var rhs = ParseExpressionAtPrecedence(nextMinPrecedence, stream, diagnostics);
                if (rhs == null)
                    return null;

                expr = new BinopExpression(expr, binop, rhs);
            }
        }

        return expr;
    }

    public static IExpression? ParseExpression(TokenStream stream, Diagnostics? diagnostics = null) => ParseExpressionAtPrecedence(0, stream, diagnostics);

    public static Module? ParseModule(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var expr = ParseExpression(stream, diagnostics);
        if (expr == null)
            return null;

        return new Module(stream.Source, new INode[] { expr });
    }
}
