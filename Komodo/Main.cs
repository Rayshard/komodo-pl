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
            Console.WriteLine(msg + Environment.NewLine);

        string message;

        switch (command)
        {
            case "":
                {
                    message = String.Join(
                        Environment.NewLine,
                        "Usage: komodo [command] [command-options] [arguments]",
                        "",
                        "Commands:",
                        "    run             Runs a program",
                        "    make-tests      Generates test files"
                    );
                }
                break;
            case "run":
                {
                    message = String.Join(
                        Environment.NewLine,
                        "Usage: komodo run [options] [input file path]",
                        "",
                        "Options:",
                        "    --print-tokens   Prints the the input file's tokens to STDOUT",
                        "    --print-cst      Prints the input file's concrete syntax tree to STDOUT"
                    );
                }
                break;
            case "make-tests":
                {
                    message = String.Join(
                        Environment.NewLine,
                        "Usage: komodo make-tests [output directory]"
                    );
                }
                break;
            default: throw new Exception($"Invalid command: {command}");
        };

        Console.WriteLine(message);

        if (exitCode.HasValue)
            Environment.Exit(exitCode.Value);
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

        var sourceFiles = new Dictionary<string, TextSource>() { { "std", new TextSource("std", "") } };
        var diagnostics = new Diagnostics();

        var sourceFileResult = TextSource.Load(inputFilePath);
        if (sourceFileResult.IsFailure)
        {
            Console.WriteLine($"ERROR: {sourceFileResult.UnwrapFailure()}");
            Environment.Exit(-1);
        }

        var sourceFile = sourceFileResult.UnwrapSuccess();
        sourceFiles.Add(sourceFile.Name, sourceFile);

        var typecheckEnvironment = new Compilation.TypeSystem.Environment();
        var operatorOverloads = new Compilation.TypeSystem.TSOperator[]
        {
            new Compilation.TypeSystem.TSOperator.BinaryAdd(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
            new Compilation.TypeSystem.TSOperator.BinarySubtract(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
            new Compilation.TypeSystem.TSOperator.BinaryMultipy(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
            new Compilation.TypeSystem.TSOperator.BinaryDivide(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
        };

        foreach (var overload in operatorOverloads)
            typecheckEnvironment.AddOperatorOverload(new Compilation.TypeSystem.Environment.OperatorOverload(overload, new TextLocation("std", 0, 0)), new TextLocation("std", 0, 0), diagnostics);

        var input = new Pass<TextSource>(sourceFile);
        var lexPass = input.Run("Lexing", Lexer.Lex, sourceFiles, diagnostics);
        var parsePass = lexPass?.Run("Parsing", Parser.ParseModule, sourceFiles, diagnostics);
        var tcPass = parsePass?.Run("TypeChecking", (input, diagnostics) => TypeChecker.TypeCheck(input, typecheckEnvironment, diagnostics), sourceFiles, diagnostics);

        if (lexPass is null || parsePass is null || tcPass is null)
        {
            diagnostics.Print(sourceFiles);
            Environment.Exit(-1);
        }

        if (printTokens)
        {
            foreach (var token in lexPass.Value)
                Console.WriteLine(token);
        }

        if (printCST)
            Console.WriteLine(JsonSerializer.Serialize(parsePass.Value));

        diagnostics.Print(sourceFiles);

        Console.WriteLine(typecheckEnvironment.ToString());

        // Run the interpreter

        Utility.PrintInfo($"Running {sourceFile.Name} ...");

        Interpreter interpreter = new Interpreter();

        var stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();

        foreach (var stmt in parsePass.Value.Statements)
        {
            var result = interpreter.Evaluate(stmt);

            if (result is KomodoException)
            {
                (result as KomodoException)!.Print(sourceFiles);
                break;
            }
        }

        stopwatch.Stop();

        Console.WriteLine(interpreter.EnvironmentToString());
        Utility.PrintInfo($"Finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");
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
        parserTestCases.Add("expr-int-literal", ("123", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
        parserTestCases.Add("expr-binop", ("1 * 4 - 7 / 6 + 9", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
        parserTestCases.Add("parenthesized-expression", ("(123 + (456 - 789))", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
        parserTestCases.Add("identifier", ("abc", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
        parserTestCases.Add("variable-declaration", ("var x = 123;", "ParseStatement", (stream, diagnostics) => Parser.ParseStatement(stream, diagnostics)));

        foreach (var (name, (input, functionName, function)) in parserTestCases)
        {
            var filePath = Path.Join(outputDirectory, $"{name}.json");

            if (File.Exists(filePath))
            {
                Utility.PrintInfo($"Test Case '{name}' already exists.");
                continue;
            }

            var source = new TextSource("test", input);
            var tokenStream = Lexer.Lex(source);

            var outputDiagnostics = new Diagnostics();
            var outputCSTNode = function(tokenStream, outputDiagnostics) ?? throw new Exception($"Could not parse cst node for test case: {name}");

            if (!outputDiagnostics.Empty)
            {
                Utility.PrintInfo($"Test Case \"{name}\" has unexpected diagnostics:");
                outputDiagnostics.Print(new Dictionary<string, TextSource>(new[] { new KeyValuePair<string, TextSource>(source.Name, source) }));
                continue;
            }

            var outputJson = new JsonObject(new[] {
                KeyValuePair.Create<string, JsonNode?>("input", JsonValue.Create(input)),
                KeyValuePair.Create<string, JsonNode?>("function", JsonValue.Create(functionName)),
                KeyValuePair.Create<string, JsonNode?>("expected", JsonSerializer.Serialize(outputCSTNode)),
            });

            File.WriteAllText(filePath, outputJson.ToString());
            Utility.PrintInfo($"Created test case '{name}'.");
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