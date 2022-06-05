namespace Komodo;

using System.Text.Json;
using Komodo.Compilation;
using Komodo.Compilation.ConcreteSyntaxTree;
using Komodo.Utilities;

static class CLI
{
    static void CheckDiagnostics(Diagnostics diagnostics)
    {
        if (diagnostics.HasError)
        {
            diagnostics.Print();
            Environment.Exit(-1);
        }
    }

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: komodo [input file path]");
            Environment.Exit(-1);
        }

        var inputFilePath = args[0];
        var sourceFileResult = SourceFile.Load(inputFilePath);
        if (sourceFileResult.IsFailure)
        {
            Console.WriteLine($"ERROR: {sourceFileResult.UnwrapFailure()}");
            Environment.Exit(-1);
        }

        var sourceFile = sourceFileResult.UnwrapSuccess();
        var diagnostics = new Diagnostics();

        var tokens = Lexer.Lex(sourceFile, diagnostics);

        CheckDiagnostics(diagnostics);

        foreach (var token in tokens)
            Console.WriteLine(token);

        var module = Parser.ParseExpression(new TokenStream(tokens), diagnostics);
        if (module == null)
            CheckDiagnostics(diagnostics);

        diagnostics.Print();
        Console.WriteLine(JsonSerializer.Serialize((ICSTNode?)module, new JsonSerializerOptions { WriteIndented = true, Converters = { new CSTNodeJsonConverter() } }));
    }
}