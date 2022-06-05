namespace Komodo;

using Komodo.Compilation;
using Komodo.Utilities;

static class CLI
{
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
        var (tokens, lexerDiagnostics) = Lexer.Lex(sourceFile);

        lexerDiagnostics.Print();
        if (lexerDiagnostics.HasError)
            Environment.Exit(-1);

        foreach (var token in tokens)
            Console.WriteLine(token);

        var (expr, parserDiagnostics) = Parser.ParseBinopExpression(new TokenStream(tokens));

        parserDiagnostics.Print();
        if (expr == null || parserDiagnostics.HasError)
            Environment.Exit(-1);

        Console.WriteLine(expr);
    }
}
