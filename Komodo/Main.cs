namespace Komodo;

using System.Text.Json.Nodes;
using Komodo.Compilation;
using Komodo.Compilation.CST;
using Komodo.Interpretation;
using Komodo.Utilities;

static class CLI
{
    static void PrintUsage(string command = "", string msg = "", int? exitCode = null)
    {
        if (msg.Length != 0)
            Console.WriteLine(msg);

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

    static void PrintInfo(string info)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[INFO] {info}");
        Console.ResetColor();
    }

    static void DoRun(IEnumerable<string> args)
    {
        if (args.Count() == 0)
            PrintUsage("run", exitCode: -1);

        var options = args.TakeWhile(x => x.StartsWith('-')).ToHashSet();

        args = args.Skip(options.Count());
        if (args.Count() != 1)
            PrintUsage("run", msg: "Expected an input file path", exitCode: -1);

        var inputFilePath = args.ElementAt(0);
        var printTokens = options.Remove("--print-tokens");
        var printCST = options.Remove("--print-cst");

        if (options.Count() != 0)
            PrintUsage("run", msg: $"Invalid option: {options.First()}", exitCode: -1);

        var sourceFileResult = TextSource.Load(inputFilePath);
        if (sourceFileResult.IsFailure)
        {
            Console.WriteLine($"ERROR: {sourceFileResult.UnwrapFailure()}");
            Environment.Exit(-1);
        }

        var sourceFiles = new Dictionary<string, TextSource>();
        var diagnostics = new Diagnostics();
        var stopwatch = new System.Diagnostics.Stopwatch();

        var sourceFile = sourceFileResult.UnwrapSuccess();
        sourceFiles.Add(sourceFile.Name, sourceFile);

        stopwatch.Start();
        var tokenStream = Lexer.Lex(sourceFile, diagnostics);
        stopwatch.Stop();

        if (diagnostics.HasError)
        {
            diagnostics.Print();
            Environment.Exit(-1);
        }

        PrintInfo($"Lexing finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");

        if (printTokens)
        {
            foreach (var token in tokenStream)
                Console.WriteLine(token);
        }

        stopwatch.Restart();

        var module = Parser.ParseModule(tokenStream, diagnostics);
        if (module == null)
        {
            diagnostics.Print();
            Environment.Exit(-1);
        }

        stopwatch.Stop();

        PrintInfo($"Parsing finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");

        if (printCST)
            Console.WriteLine(JsonSerializer.Serialize(module));

        diagnostics.Print();

        PrintInfo($"Running {sourceFile.Name} ...");

        Interpreter interpreter = new Interpreter();

        stopwatch.Restart();

        foreach (var node in module.Nodes)
        {
            var result = interpreter.Evaluate(node);
            if (result is KomodoException)
            {
                (result as KomodoException)?.Print(sourceFiles);
                break;
            }

            Console.WriteLine(result);
        }

        stopwatch.Stop();
        PrintInfo($"Finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");
    }

    static void DoMakeTests(IEnumerable<string> args)
    {
        if (args.Count() != 1)
            PrintUsage("make-tests", msg: "Expected an output directory", exitCode: -1);

        var outputDirectory = args.ElementAt(0);

        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Specified directory {outputDirectory} is not valid or does not exist!");
            Environment.Exit(-1);
        }

        // Parser Tests
        var parserTestCases = new Dictionary<string, (string, string, Func<TokenStream, Diagnostics?, INode?>)>();
        parserTestCases.Add("expr-int-literal", ("123", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics, 0)));
        parserTestCases.Add("expr-binop", ("1 * 4 - 7 / 6 + 9", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics, 0)));
        parserTestCases.Add("parenthesized-expression", ("(123 + (456 - 789))", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics, 0)));

        foreach (var (name, (input, functionName, function)) in parserTestCases)
        {
            var filePath = Path.Join(outputDirectory, $"{name}.json");

            if (File.Exists(filePath))
                continue;

            var tokenStream = Lexer.Lex(new TextSource("test", input));

            var outputDiagnostics = new Diagnostics();
            var outputCSTNode = function(tokenStream, outputDiagnostics) ?? throw new Exception($"Could not parse cst node for test case: {name}");

            if (!outputDiagnostics.Empty)
            {
                Console.WriteLine($"Test Case \"{name}\" has unexpected diagnostics:");
                outputDiagnostics.Print();
                continue;
            }

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
            PrintUsage("", msg: "Expected a command", exitCode: -1);

        var command = remainingArgs.ElementAt(0);
        remainingArgs = remainingArgs.Skip(1);

        switch (command)
        {
            case "run": DoRun(remainingArgs); break;
            case "make-tests": DoMakeTests(remainingArgs); break;
            default: PrintUsage("", msg: $"Unknown command: {command}", exitCode: -1); break;
        }
    }
}