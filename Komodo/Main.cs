namespace Komodo;

using System.Text.Json.Nodes;
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

    static void PrintUsage(string command = "", int? exitCode = null)
    {
        switch (command)
        {
            case "": Console.WriteLine("Usage: komodo [command] [command-options] [arguments]"); break;
            case "run": Console.WriteLine("Usage: komodo run [input file path]"); break;
            case "make-tests": Console.WriteLine("Usage: komodo make-tests [output directory]"); break;
            default: throw new Exception($"Invalid command: {command}");
        };

        if (exitCode.HasValue)
            Environment.Exit(exitCode.Value);
    }

    static void DoRun(IEnumerable<string> args)
    {
        if (args.Count() != 1)
            PrintUsage("run", -1);

        var inputFilePath = args.ElementAt(0);
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

        var expression = Parser.ParseExpression(new TokenStream(tokens), diagnostics);
        if (expression == null)
        {
            CheckDiagnostics(diagnostics);
            return;
        }


        //var json = JsonSerializer.Serialize((ICSTNode?)expression, new JsonSerializerOptions { Converters = { new CSTNodeJsonConverter() } });
        var json = JsonSerializer.Serialize(expression);
        var deserialized = JsonSerializer.ParseCSTNode(JsonNode.Parse(json.ToString()) ?? throw new Exception("Invalid json"));
        Console.WriteLine(expression.Equals(deserialized));

        diagnostics.Print();
    }

    static void DoMakeTests(IEnumerable<string> args)
    {
        if (args.Count() != 1)
            PrintUsage("make-tests", -1);

        var outputDirectory = args.ElementAt(0);

        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Specified directory {outputDirectory} is not valid or does not exist!");
            Environment.Exit(-1);
        }

        // Parser Tests
        var parserTestCases = new Dictionary<string, (string, string, Func<TokenStream, Diagnostics?, ICSTNode?>)>();
        parserTestCases.Add("expr-int-literal", ("123", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics, 0)));
        parserTestCases.Add("expr-binop", ("123+456\n", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics, 0)));

        foreach (var (name, (input, functionName, function)) in parserTestCases)
        {
            var filePath = Path.Join(outputDirectory, $"{name}.json");
            var tokenStream = new TokenStream(Lexer.Lex(new SourceFile("test", input)));
            var outputCSTNode = function(tokenStream, null) ?? throw new Exception($"Could not parse cst node from input: {input}");

            var outputJson = new JsonObject(new[] {
                KeyValuePair.Create<string, JsonNode?>("input", JsonValue.Create(input)),
                KeyValuePair.Create<string, JsonNode?>("function", JsonValue.Create(functionName)),
                KeyValuePair.Create<string, JsonNode?>("expected", JsonSerializer.Serialize(outputCSTNode)),
            });

            File.WriteAllText(filePath, outputJson.ToString());
        }
    }

    static void Main(string[] args)
    {
        var remainingArgs = args.AsEnumerable();
        if (remainingArgs.Count() == 0)
            PrintUsage("", -1);

        var command = remainingArgs.ElementAt(0);
        remainingArgs = remainingArgs.Skip(1);

        switch (command)
        {
            case "run": DoRun(remainingArgs); break;
            case "make-tests": DoMakeTests(remainingArgs); break;
            default:
                {
                    Console.WriteLine($"Unknown command: {command}");
                    PrintUsage("", -1);
                }
                break;
        }
    }
}