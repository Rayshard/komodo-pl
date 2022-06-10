namespace Komodo.Compilation;

using System.Diagnostics;
using Komodo.Utilities;

public enum TokenType
{
    Invalid,
    IntLit,
    Plus,
    Minus,
    Asterisk,
    ForwardSlash,
    LParen,
    RParen,
    EOF,
}

public record Token(TokenType Type, Location Location, string Value)
{
    public override string ToString() => $"{Type.ToString()}({Value}) at {Location}";
}

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
        set => _offset = Math.Max(0, Math.Min(value, _tokens.Length - 1));
    }
}