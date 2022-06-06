namespace Komodo.Tests.Compilation;

using Komodo.Compilation;
using Komodo.Utilities;

public class ParserTest
{
    [Theory]
    [MemberData(nameof(GetTokens))]
    public void CorrectParsedCSTNode(string input, string _other)[] expected)
    {
        var sf = new SourceFile("Test", input);
        var diagnostics = new Diagnostics();
        var expectedTokens = new List<Token>(expected.Select(e => new Token(e.type, sf.GetLocation(e.start, e.end), sf.Text.Substring(e.start, e.end - e.start))));
        var actualTokens = Lexer.Lex(sf, diagnostics);

        Assert.Equal(expectedTokens, actualTokens);
        Assert.True(diagnostics.Empty);
    }

    

    public static IEnumerable<object[]> GetTokens()
    {
        foreach (var (input, expected) in TokensData)
            yield return new object[] { input, expected };
    }
}