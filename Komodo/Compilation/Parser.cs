namespace Komodo.Compilation;

using System.Diagnostics;
using Komodo.Compilation.Syntax;
using Komodo.Utilities;

public class TokenStream
{
    private Token[] _tokens;
    private int _offset;

    public TokenStream(IEnumerable<Token> tokens)
    {
        _tokens = tokens.ToArray();
        _offset = 0;

        Trace.Assert(_tokens.Count() > 0, "Input tokens must have at least one token!");
        Trace.Assert(_tokens.Last().Type == TokenType.EOF, $"Expected the last token to be an EOF but found {_tokens.Last()}");
    }

    public Token Next()
    {
        var token = _tokens[_offset];

        if (token.Type != TokenType.EOF)
            _offset++;

        return token;
    }

    public Token Peek() => _tokens[_offset];

    public int Offset
    {
        get => _offset;
        set => _offset = Math.Max(0, Math.Min(_offset, _tokens.Length - 1));
    }
}

public static class ParseError
{
    public static Diagnostic UnexpectedToken(Token token) => new Diagnostic(DiagnosticType.Error, token.Location, $"Encountered unexpected token: {token.Type}({token.Value})");
}

public static class Parser
{
    public static (BinaryOperator?, Diagnostics) ParseBinaryOperator(TokenStream stream)
    {
        var diagnostics = new Diagnostics();
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.Plus:
            case TokenType.Minus:
            case TokenType.Asterisk:
            case TokenType.ForwardSlash:
                return (new BinaryOperator(token), diagnostics);
            default:
                diagnostics.Add(ParseError.UnexpectedToken(token));
                return (null, diagnostics);
        }
    }

    public static (Expression?, Diagnostics) ParseAtom(TokenStream stream)
    {
        var diagnostics = new Diagnostics();
        var token = stream.Next();

        switch (token.Type)
        {
            case TokenType.IntLit:
                return (new IntegerLiteral(token), diagnostics);
            default:
                diagnostics.Add(ParseError.UnexpectedToken(token));
                return (null, diagnostics);
        }
    }

    public static (BinopExpression?, Diagnostics) ParseBinopExpression(TokenStream stream)
    {
        var diagnostics = new Diagnostics();

        //Parse Left Expression
        var (left, leftDiagnostics) = ParseAtom(stream);

        diagnostics.Append(leftDiagnostics);
        if (left == null || diagnostics.HasError)
            return (null, diagnostics);

        //Parse Binary Operator
        var (op, opDiagnostics) = ParseBinaryOperator(stream);

        diagnostics.Append(opDiagnostics);
        if (op == null || diagnostics.HasError)
            return (null, diagnostics);

        //Parse Right Expression
        var (right, rightDiagnostics) = ParseAtom(stream);

        diagnostics.Append(rightDiagnostics);
        if (right == null || diagnostics.HasError)
            return (null, diagnostics);

        return (new BinopExpression(left, op, right), diagnostics);
    }
}
