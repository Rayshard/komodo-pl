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
