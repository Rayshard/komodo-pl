namespace Komodo.Tests.Compilation;

using Komodo.Compilation;
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
        var sf = new SourceFile("Test", input);
        var diagnostics = new Diagnostics();
        var expectedTokens = new List<Token>(expected.Select(e => new Token(e.type, sf.GetLocation(e.start, e.end), sf.Text.Substring(e.start, e.end - e.start))));
        var actualTokens = Lexer.Lex(sf, diagnostics);

        Assert.Equal(expectedTokens, actualTokens);
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
    };

    public static IEnumerable<object[]> GetTokens()
    {
        foreach (var (input, expected) in TokensData)
            yield return new object[] { input, expected };
    }
}