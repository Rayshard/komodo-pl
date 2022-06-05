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
    public static (T?, Diagnostics) Try<T>(Func<TokenStream, (T? Node, Diagnostics Diagnostics)> parseFunc, TokenStream stream)
    {
        var streamStart = stream.Offset;
        var result = parseFunc(stream);

        if (result.Node == null)
            stream.Offset = streamStart;

        return result;
    }

    public static (CSTBinop?, Diagnostics) ParseBinop(TokenStream stream)
    {
        var diagnostics = new Diagnostics();
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.Plus:
            case TokenType.Minus:
            case TokenType.Asterisk:
            case TokenType.ForwardSlash:
                return (new CSTBinop(token), diagnostics);
            default:
                diagnostics.Add(ParseError.UnexpectedToken(token));
                return (null, diagnostics);
        }
    }

    public static (CSTLiteral?, Diagnostics) ParseLiteral(TokenStream stream)
    {
        var diagnostics = new Diagnostics();
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.IntLit:
                return (new CSTLiteral(token), diagnostics);
            default:
                diagnostics.Add(ParseError.UnexpectedToken(token));
                return (null, diagnostics);
        }
    }

    public static (ICSTExpression?, Diagnostics) ParseAtom(TokenStream stream) => ParseLiteral(stream);

    public static (ICSTExpression?, Diagnostics) ParseExpression(TokenStream stream, int precedence = 0)
    {
        var (expr, diagnostics) = Try(ParseAtom, stream);

        if (expr != null)
        {
            while (true)
            {
                var (binop, binopDiagnostics) = Try(ParseBinop, stream);
                if (binop == null || binop.Precedence < precedence)
                    break;

                var nextPrecedence = binop.Asssociativity == BinaryOperationAssociativity.Right ? binop.Precedence : (binop.Precedence + 1);
                var (rhs, rhsDiagnostics) = Try((s) => ParseExpression(s, nextPrecedence), stream);
                
                diagnostics.Append(rhsDiagnostics);
                
                if (rhs == null)
                    return (null, diagnostics);

                expr = new CSTBinopExpression(expr, binop, rhs);
            }
        }

        return (expr, diagnostics);
    }

    public static (CSTBinopExpression?, Diagnostics) ParseBinopExpression(TokenStream stream)
    {
        var diagnostics = new Diagnostics();

        //Parse Left Expression
        var (left, leftDiagnostics) = ParseLiteral(stream);

        diagnostics.Append(leftDiagnostics);
        if (left == null || diagnostics.HasError)
            return (null, diagnostics);

        //Parse Binary Operator
        var (op, opDiagnostics) = ParseBinop(stream);

        diagnostics.Append(opDiagnostics);
        if (op == null || diagnostics.HasError)
            return (null, diagnostics);

        //Parse Right Expression
        var (right, rightDiagnostics) = ParseLiteral(stream);

        diagnostics.Append(rightDiagnostics);
        if (right == null || diagnostics.HasError)
            return (null, diagnostics);

        return (new CSTBinopExpression(left, op, right), diagnostics);
    }
}
