namespace Komodo.Compilation;

using System.Diagnostics;
using Komodo.Compilation.ConcreteSyntaxTree;
using Komodo.Utilities;


public static class ParseError
{
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

    public static CSTBinaryOperator? ParseBinop(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.Plus:
            case TokenType.Minus:
            case TokenType.Asterisk:
            case TokenType.ForwardSlash:
                return new CSTBinaryOperator(token);
            default:
                diagnostics?.Add(ParseError.UnexpectedToken(token));
                return null;
        }
    }

    public static CSTLiteral? ParseLiteral(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.IntLit: return new CSTLiteral(token);
            default:
                diagnostics?.Add(ParseError.UnexpectedToken(token));
                return null;
        }
    }

    public static ICSTExpression? ParseAtom(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Peek();

        switch (token.Type)
        {
            default: return ParseLiteral(stream, diagnostics);
        }
    }

    public static ICSTExpression? ParseExpression(TokenStream stream, Diagnostics? diagnostics = null, int minPrecedence = 0)
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

                expr = new CSTBinopExpression(expr, binop, rhs);
            }
        }

        return expr;
    }
}
