namespace Komodo.Compilation;

using Komodo.Compilation.CST;
using Komodo.Utilities;

public static class ParseError
{
    public static Diagnostic ExpectedToken(TokenType expected, Token found) => new Diagnostic(DiagnosticType.Error, found.Location, $"Expected {expected} but found {found.Type}({found.Value})");
    public static Diagnostic UnexpectedToken(Token token) => new Diagnostic(DiagnosticType.Error, token.Location, $"Encountered unexpected token: {token.Type}({token.Value})");
}

public static class Parser
{
    public static T? Try<T>(Func<TokenStream, Diagnostics?, T?> parseFunc, TokenStream stream, Diagnostics? diagnostics = null)
    {
        var streamStart = stream.Offset;
        var node = parseFunc(stream, diagnostics);

        if (node == null)
            stream.Offset = streamStart;

        return node;
    }

    public static Token? ExpectToken(TokenType type, TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        if (token.Type != type)
        {
            stream.Offset -= 1; // Unconsume token
            diagnostics?.Add(ParseError.ExpectedToken(type, token));
            return null;
        }

        return token;
    }

    public static BinaryOperator? ParseBinop(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.Plus:
            case TokenType.Minus:
            case TokenType.Asterisk:
            case TokenType.ForwardSlash:
                return new BinaryOperator(token);
            default:
                diagnostics?.Add(ParseError.UnexpectedToken(token));
                return null;
        }
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

    public static IExpression? ParseAtom(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Peek();

        switch (token.Type)
        {
            case TokenType.LParen:
                {
                    var lParen = stream.Next();

                    var expr = ParseExpression(stream, diagnostics);
                    if (expr == null)
                        return null;

                    var rParen = ExpectToken(TokenType.RParen, stream, diagnostics);
                    if (rParen == null)
                        return null;

                    return new ParenthesizedExpression(lParen, expr, rParen);
                }
            default: return ParseLiteral(stream, diagnostics);
        }
    }

    public static IExpression? ParseExpression(TokenStream stream, Diagnostics? diagnostics = null, int minPrecedence = 0)
    {
        var expr = Try(ParseAtom, stream, diagnostics);

        if (expr != null)
        {
            while (true)
            {
                var streamStart = stream.Offset;

                var binop = Try(ParseBinop, stream, null);
                if (binop == null || binop.Precedence < minPrecedence)
                {
                    stream.Offset = streamStart;
                    break;
                }

                var nextMinPrecedence = binop.Asssociativity == BinaryOperationAssociativity.Right ? binop.Precedence : (binop.Precedence + 1);
                var rhs = Try((s, d) => ParseExpression(s, d, nextMinPrecedence), stream, diagnostics);
                if (rhs == null)
                    return null;

                expr = new BinopExpression(expr, binop, rhs);
            }
        }

        return expr;
    }

    public static Module? ParseModule(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var lBracket = ExpectToken(TokenType.LCBracket, stream, diagnostics);
        if (lBracket == null)
            return null;

        var expr = ParseExpression(stream, diagnostics);
        if (expr == null)
            return null;

        var rBracket = ExpectToken(TokenType.RCBracket, stream, diagnostics);
        if (rBracket == null)
            return null;

        return new Module(stream.Source, new INode[] { expr });
    }
}
