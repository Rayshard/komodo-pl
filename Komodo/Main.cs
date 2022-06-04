namespace Komodo;

using Komodo.Compilation;
using Komodo.Utilities;

class CLI
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

        var diagnostics = new Diagnostics();

        var sourceFile = sourceFileResult.UnwrapSuccess();
        var tokens = Lexer.Lex(sourceFile, diagnostics);

        diagnostics.Print();

        if (diagnostics.HasError)
            Environment.Exit(-1);

        foreach (var token in tokens)
            Console.WriteLine(token);
    }
}
