namespace Komodo.Tests.Compilation;

using System.Text.Json.Nodes;
using Komodo.Compilation;
using Komodo.Compilation.ConcreteSyntaxTree;
using Komodo.Utilities;

public class ParserTest
{
    [Theory]
    [MemberData(nameof(GetTestCaseFilePaths))]
    public void CorrectlyParsedCSTNode(string filePath)
    {
        using StreamReader stream = new StreamReader(filePath);

        var json = JsonNode.Parse(stream.BaseStream) ?? throw new Exception($"File at {filePath} is not valid json!");
        var testCase = json.AsObject();

        var input = testCase["input"]?.GetValue<string>() ?? throw new Exception("Expected a string");
        var function = testCase["function"]?.GetValue<string>() ?? throw new Exception("Expected a string");
        var expected = testCase["expected"]?.AsObject() ?? throw new Exception("Expected an object");

        var actualTokenStream = new TokenStream(Lexer.Lex(new SourceFile("test", input), null));
        var actualDiagnostics = new Diagnostics();
        ICSTNode? actual = function switch
        {
            "ParseExpression" => Parser.ParseExpression(actualTokenStream, actualDiagnostics),
            _ => throw new Exception($"Unknown parse function: {function}")
        };

        Assert.Equal(JsonSerializer.ParseCSTNode(expected), actual);
    }

    public static IEnumerable<object[]> GetTestCaseFilePaths()
    {
        foreach(var filePath in Directory.GetFiles("tests/"))
            yield return new object[] { filePath }; 
    }
}