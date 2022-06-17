namespace Komodo.Compilation;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Komodo.Compilation.CST;
using Komodo.Utilities;

public class TokenStream : IEnumerable<Token>
{
    private Token[] _tokens;
    private int _offset;

    public TextSource Source { get; }

    public TokenStream(TextSource source, IEnumerable<Token> tokens)
    {
        Source = source;

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

    public IEnumerator<Token> GetEnumerator() => _tokens.ToList().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public int Offset
    {
        get => _offset;
        set => _offset = Math.Max(0, Math.Min(value, _tokens.Length - 1));
    }
}

public static class Lexer
{
    static readonly Regex RE_WHITESPACE = new Regex(@"\G\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly ReadOnlyCollection<(Regex, Func<string, TokenType>)> PATTERNS = Array.AsReadOnly(new (Regex, Func<string, TokenType>)[]
    {
            (
                new Regex(@"\G([0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                _ => TokenType.IntLit
            ),
            (
                new Regex(@"\G(\+|-|\*|/|\(|\)|\{|\})", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                text => text switch
                    {
                        "+" => TokenType.Plus,
                        "-" => TokenType.Minus,
                        "*" => TokenType.Asterisk,
                        "/" => TokenType.ForwardSlash,
                        "(" => TokenType.LParen,
                        ")" => TokenType.RParen,
                        "{" => TokenType.LCBracket,
                        "}" => TokenType.RCBracket,
                        _ => throw new ArgumentException(text)
                    }
            ),
            (
                new Regex(@"\G([\s\S])", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                _ => TokenType.Invalid
            ),
    });

    public static TokenStream Lex(TextSource source, Diagnostics? diagnostics = null)
    {
        var tokens = new List<Token>();
        int offset = 0;

        while (offset < source.Length)
        {
            //Skip Whitespace
            offset += RE_WHITESPACE.Match(source.Text, offset).Length;
            if (offset >= source.Length)
                break;

            //Find best match
            (string, Func<string, TokenType>)? bestMatch = null;

            foreach (var (pattern, func) in PATTERNS)
            {
                var match = pattern.Match(source.Text, offset);
                if (!match.Success)
                    continue;

                if (!bestMatch.HasValue || match.Value.Length > bestMatch.Value.Item1.Length)
                    bestMatch = (match.Value, func);
            }

            var (value, f) = bestMatch ?? throw new Exception($"'{source.Text.Substring(offset)}' did not match any pattern!");
            var token = new Token(f(value), new TextLocation(source.Name, offset, offset + value.Length), value);

            if (token.Type == TokenType.Invalid) { diagnostics?.Add(new Diagnostic(DiagnosticType.Error, token.Location, $"Encounterd an invalid token: {token.Value}")); }
            else { tokens.Add(token); }

            offset += value.Length;
        }

        tokens.Add(new Token(TokenType.EOF, new TextLocation(source.Name, offset, offset), ""));
        return new TokenStream(source, tokens);
    }
}
