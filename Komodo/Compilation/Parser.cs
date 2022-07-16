using Komodo.Compilation.CST;
using Komodo.Utilities;
using System.Collections.ObjectModel;

namespace Komodo.Compilation;

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
            diagnostics?.Add(Error.ParserExpectedToken(type, token));
            return null;
        }

        return token;
    }

    public static Identifier? ParseIdentifier(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var id = ExpectToken(TokenType.Identifier, stream, diagnostics);
        if (id == null)
            return null;

        return new Identifier(id);
    }

    public static BinaryOperator? ParseBinop(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var token = stream.Next();

        if (!BinaryOperatorTokens.Contains(token.Type))
        {
            diagnostics?.Add(Error.ParserUnexpectedToken(token));
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
                diagnostics?.Add(Error.ParserUnexpectedToken(token));
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

    public static VariableDeclaration? ParseVariableDeclaration(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var varKeyword = ExpectToken(TokenType.KW_VAR, stream, diagnostics);
        if (varKeyword == null)
            return null;

        var id = ExpectToken(TokenType.Identifier, stream, diagnostics);
        if (id == null)
            return null;

        var singleEqualsSymbol = ExpectToken(TokenType.SingleEquals, stream, diagnostics);
        if (singleEqualsSymbol == null)
            return null;

        var expr = ParseExpression(stream, diagnostics);
        if (expr == null)
            return null;

        var semicolon = ExpectToken(TokenType.Semicolon, stream, diagnostics);
        if (semicolon == null)
            return null;

        return new VariableDeclaration(varKeyword, id, singleEqualsSymbol, expr, semicolon);
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

        (atom, var IdentifierDiagnostics) = Try(ParseIdentifier, stream);
        if (atom != null)
        {
            diagnostics?.Append(IdentifierDiagnostics);
            return atom;
        }

        diagnostics?.Add(Error.ParserUnexpectedToken(stream.Next()));
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

    public static IStatement? ParseStatement(TokenStream stream, Diagnostics? diagnostics = null) => ParseVariableDeclaration(stream, diagnostics);

    public static Module? ParseModule(TokenStream stream, Diagnostics? diagnostics = null)
    {
        var stmts = new List<IStatement>();

        while (stream.Peek().Type != TokenType.EOF)
        {
            var stmt = ParseStatement(stream, diagnostics);
            if (stmt == null)
                break;

            stmts.Add(stmt);
        }

        var eof = ExpectToken(TokenType.EOF, stream, diagnostics);
        if (eof == null)
            return null;

        return new Module(stmts.ToArray(), eof);
    }
}
