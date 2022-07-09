namespace Komodo.Tests.Compilation;

using Komodo.Compilation;
using Komodo.Compilation.CST;
using Komodo.Utilities;

public class LexerTest
{
    [Fact]
    public void AllPatternsStartWithSlashG()
    {
        foreach (var (pattern, _) in Lexer.PATTERNS)
            Assert.True(pattern.ToString().StartsWith(@"\G"), $"The pattern {pattern} does not start with a '\\G'.");
    }

    [Theory]
    [MemberData(nameof(GetTokens))]
    public void CorrectTokens(string input, (TokenType type, int start, int end)[] expected)
    {
        var source = new TextSource("Test", input);
        var diagnostics = new Diagnostics();
        var expectedTokens = new List<Token>(expected.Select(e => new Token(e.type, new TextLocation(source.Name, e.start, e.end), source.Text.Substring(e.start, e.end - e.start))));
        var actualTokens = Lexer.Lex(source, diagnostics);

        Assert.Equal(expectedTokens.AsEnumerable(), actualTokens.AsEnumerable());
        Assert.True(diagnostics.Empty);
    }

    public static (string Input, (TokenType Type, int start, int end)[] Expected)[] TokensData => new[]
    {
        ("", new[] {(TokenType.EOF, 0, 0)}),
        ("123", new[] {(TokenType.IntLit, 0, 3), (TokenType.EOF, 3, 3)}),
        ("+", new[] {(TokenType.Plus, 0, 1), (TokenType.EOF, 1, 1)}),
        ("-", new[] {(TokenType.Minus, 0, 1), (TokenType.EOF, 1, 1)}),
        ("*", new[] {(TokenType.Asterisk, 0, 1), (TokenType.EOF, 1, 1)}),
        ("/", new[] {(TokenType.ForwardSlash, 0, 1), (TokenType.EOF, 1, 1)}),
        ("(", new[] {(TokenType.LParen, 0, 1), (TokenType.EOF, 1, 1)}),
        (")", new[] {(TokenType.RParen, 0, 1), (TokenType.EOF, 1, 1)}),
        ("{", new[] {(TokenType.LCBracket, 0, 1), (TokenType.EOF, 1, 1)}),
        ("}", new[] {(TokenType.RCBracket, 0, 1), (TokenType.EOF, 1, 1)}),
        (";", new[] {(TokenType.Semicolon, 0, 1), (TokenType.EOF, 1, 1)}),
        ("=", new[] {(TokenType.SingleEquals, 0, 1), (TokenType.EOF, 1, 1)}),

        // Identifier
        ("_", new[] {(TokenType.Identifier, 0, 1), (TokenType.EOF, 1, 1)}),
        ("__", new[] {(TokenType.Identifier, 0, 2), (TokenType.EOF, 2, 2)}),
        ("_1", new[] {(TokenType.Identifier, 0, 2), (TokenType.EOF, 2, 2)}),
        ("_1a", new[] {(TokenType.Identifier, 0, 3), (TokenType.EOF, 3, 3)}),
        ("_a1", new[] {(TokenType.Identifier, 0, 3), (TokenType.EOF, 3, 3)}),
        ("_a1_", new[] {(TokenType.Identifier, 0, 4), (TokenType.EOF, 4, 4)}),
        ("w", new[] {(TokenType.Identifier, 0, 1), (TokenType.EOF, 1, 1)}),
        ("w1", new[] {(TokenType.Identifier, 0, 2), (TokenType.EOF, 2, 2)}),
        ("w1_", new[] {(TokenType.Identifier, 0, 3), (TokenType.EOF, 3, 3)}),
        ("w1_1p", new[] {(TokenType.Identifier, 0, 5), (TokenType.EOF, 5, 5)}),
        ("alpha'", new[] {(TokenType.Identifier, 0, 6), (TokenType.EOF, 6, 6)}),
        ("alpha''", new[] {(TokenType.Identifier, 0, 7), (TokenType.EOF, 7, 7)}),
        ("alpha1'", new[] {(TokenType.Identifier, 0, 7), (TokenType.EOF, 7, 7)}),
        ("alpha1_'", new[] {(TokenType.Identifier, 0, 8), (TokenType.EOF, 8, 8)}),

        // Keywords
        ("var", new[] {(TokenType.KW_VAR, 0, 3), (TokenType.EOF, 3, 3)}),

        // Combinations
        ("1_", new[] {(TokenType.IntLit, 0, 1), (TokenType.Identifier, 1, 2), (TokenType.EOF, 2, 2)}),
        ("7_a", new[] {(TokenType.IntLit, 0, 1), (TokenType.Identifier, 1, 3), (TokenType.EOF, 3, 3)}),
        ("alpha'beta'", new[] {(TokenType.Identifier, 0, 6), (TokenType.Identifier, 6, 11), (TokenType.EOF, 11, 11)}),
        ("alpha''beta'", new[] {(TokenType.Identifier, 0, 7), (TokenType.Identifier, 7, 12), (TokenType.EOF, 12, 12)}),
        ("alpha'7beta'", new[] {(TokenType.Identifier, 0, 6), (TokenType.IntLit, 6, 7), (TokenType.Identifier, 7, 12), (TokenType.EOF, 12, 12)}),
    };

    public static IEnumerable<object[]> GetTokens()
    {
        foreach (var (input, expected) in TokensData)
            yield return new object[] { input, expected };
    }
}