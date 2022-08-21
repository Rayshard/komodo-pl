namespace Komodo;

using System.Text.RegularExpressions;
using Komodo.Compilation;
using Komodo.Compilation.CST;
using Komodo.Interpretation;
using Komodo.Utilities;
using Newtonsoft.Json.Linq;

abstract record Option(string Name)
{
    public record Flag(string Name) : Option(Name)
    {
        static Regex Pattern = new Regex("^--(?<Name>([a-zA-Z]+))$");

        new public static Option? Parse(string text)
        {
            var match = Pattern.Match(text);
            return match.Success ? new Flag(match.Groups["Name"].Value) : null;
        }
    }

    public record Parameter(string Name, string Value) : Option(Name)
    {
        static Regex Pattern = new Regex("--(?<Name>([a-zA-Z]+))=(?<Value>([a-zA-Z]+))");

        new public static Option? Parse(string text)
        {
            var match = Pattern.Match(text);
            return match.Success ? new Parameter(match.Groups["Name"].Value, match.Groups["Value"].Value) : null;
        }
    }

    public static Option? Parse(string text)
    {
        var flag = Flag.Parse(text);
        if (flag is not null)
            return flag;

        var parameter = Parameter.Parse(text);
        if (parameter is not null)
            return parameter;

        return null;
    }
}

static class Entry
{
    static Regex CreateSettableOptionRegex(string name, Regex valueRegex) => new Regex($"--{name}=(?<Value>({valueRegex.ToString()}))");

    static void PrintUsage(string command = "", string msg = "", int? exitCode = null)
    {
        string message = "";

        if (msg.Length != 0)
            message += msg + Environment.NewLine;

        switch (command)
        {
            case "":
                {
                    message += String.Join(
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
                    message += String.Join(
                        Environment.NewLine,
                        "Usage: komodo run [options] [input file path]",
                        "",
                        "Options:",
                        "    --loglevel       Sets the log level. Default is NOLOG",
                        "    --print-tokens   Prints the the input file's tokens to STDOUT",
                        "    --print-cst      Prints the input file's concrete syntax tree to STDOUT"
                    );
                }
                break;
            case "run-ir":
                {
                    message += String.Join(
                        Environment.NewLine,
                        "Usage: komodo run-ir [input file path]"
                    );
                }
                break;
            case "make-tests":
                {
                    message += String.Join(
                        Environment.NewLine,
                        "Usage: komodo make-tests [output directory]"
                    );
                }
                break;
            default: throw new Exception($"Invalid command: {command}");
        };

        Logger.Error(message);

        if (exitCode.HasValue)
            Environment.Exit(exitCode.Value);
    }

    static Compilation.AST.Module? CompileSource(TextSource source, Compilation.TypeSystem.Environment tcEnv, Diagnostics? diagnostics)
    {
        return new Pass<TextSource>(source)
                .Bind("Lexing", Lexer.Lex, diagnostics)
                .Bind("Parsing", Parser.ParseModule, diagnostics)
                .Bind("TypeChecking", (input, diagnostics) => TypeChecker.TypeCheck(input, tcEnv, diagnostics), diagnostics).Value;
    }

    static (Dictionary<string, Option> Options, IEnumerable<string> Remainder) ParseOptions(IEnumerable<string> args)
    {
        var options = new Dictionary<string, Option>();
        var items = args.TakeWhile(x => x.StartsWith('-')).ToHashSet();

        foreach (var item in items)
        {
            var option = Option.Parse(item);
            if (option is null)
                throw new Exception($"Cannot parse option: {item}");

            options.Add(option.Name, option);
        }

        return (options, args.Skip(options.Count));
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

        var logLevelRegex = CreateSettableOptionRegex("loglevel", new Regex(String.Join('|', Enum.GetNames(typeof(LogLevel)))));
        var logLevelOptionMatches = options.Select(x => logLevelRegex.Match(x)).TakeWhile(x => x.Success);
        foreach (var llom in logLevelOptionMatches)
        {
            var value = llom.Groups["Value"].Value;
            Logger.MinLevel = (LogLevel)Enum.Parse(typeof(LogLevel), value);
            options.Remove(llom.Value);
        }

        if (options.Count() != 0)
            PrintUsage("run", msg: $"Invalid option: {options.First()}", exitCode: -1);

        var sourceMap = new Dictionary<string, TextSource>() { { "std", new TextSource("std", "") } };

        try
        {
            var source = TextSource.Load(inputFilePath);
            sourceMap.Add(source!.Name, source);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
            Environment.Exit(1);
        }

        var module = Compiler.Compile(sourceMap, inputFilePath, printTokens, printCST);
        if (module is null)
        {
            Logger.Error("Compilation was unsuccessful. Check diagnostics for further information.");
            Environment.Exit(1);
        }
    }

    static void DoRunIR(IEnumerable<string> args)
    {
        if (args.Count() == 0)
            PrintUsage("run-ir", exitCode: -1);

        if (args.Count() != 1)
            PrintUsage("run-ir", msg: "Expected one input file path", exitCode: -1);

        var inputFilePath = args.ElementAt(0);

        if (!File.Exists(inputFilePath)) { PrintUsage("run-ir", msg: $"File does not exist at {inputFilePath}", exitCode: -1); }
        else if (!inputFilePath.EndsWith(".kmd.ir.json")) { PrintUsage("run-ir", msg: "Expected a komodo ir file (a file ending in .kmd.ir.json)", exitCode: -1); }

        Compilation.Bytecode.Program? program = null;

        try
        {
            var json = JToken.Parse(File.ReadAllText(inputFilePath))!;
            program = Compilation.Bytecode.Formatter.DeserializeProgram(json);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message + "\n" + e.StackTrace);
            PrintUsage("run-ir", msg: "Expected a valid komodo ir file", exitCode: -1);
        }

        if (program is null)
            throw new NullReferenceException();

        //Console.WriteLine(Utility.ToFormattedString(Compilation.Bytecode.Formatter.Serialize(program)));

        var interpreter = new Interpreter(program);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var exitcode = interpreter.Run();
        stopwatch.Stop();

        Logger.Info($"Exited with code {exitcode}");
        Logger.Info($"Finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");

        Environment.Exit((int)exitcode);
    }

    static void DoMakeTests(IEnumerable<string> args)
    {
        if (args.Count() != 1)
            PrintUsage("make-tests", msg: "Expected an output directory", exitCode: -1);

        var outputDirectory = args.ElementAt(0);

        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Specified directory {outputDirectory} is not valid or does not exist!");
            Environment.Exit(1);
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
                Logger.Info($"Test Case '{name}' already exists.");
                continue;
            }

            var source = new TextSource("test", input);
            var tokenStream = Lexer.Lex(source);

            var outputDiagnostics = new Diagnostics();
            var outputCSTNode = function(tokenStream, outputDiagnostics) ?? throw new Exception($"Could not parse cst node for test case: {name}");

            if (!outputDiagnostics.Empty)
            {
                Logger.Info($"Test Case \"{name}\" has unexpected diagnostics:");
                outputDiagnostics.Print(new Dictionary<string, TextSource>(new[] { new KeyValuePair<string, TextSource>(source.Name, source) }));
                continue;
            }

            var outputJson = new JObject();
            outputJson.Add("input", input);
            outputJson.Add("function", functionName);
            outputJson.Add("expected", JToken.Parse(JsonSerializer.Serialize(outputCSTNode).ToJsonString()));

            File.WriteAllText(filePath, outputJson.ToString());
            Logger.Info($"Created test case '{name}'.");
        }
    }

    static void Main(string[] args)
    {
        Logger.MinLevel = LogLevel.ERROR;

        var (options, remainingArgs) = ParseOptions(args);

        if (options.ContainsKey("loglevel"))
        {
            var option = options["loglevel"];

            try
            {
                if (option is Option.Parameter parameter)
                {
                    LogLevel level;
                    if (!Enum.TryParse<LogLevel>(parameter.Value, out level))
                        throw new Exception($"'loglevel' can only be one of {String.Join('|', Enum.GetNames(typeof(LogLevel)))}");

                    Logger.MinLevel = level;
                }
                else { throw new Exception("'loglevel' is a parameter"); }
            }
            catch (Exception e) { PrintUsage(msg: $"Invalid option: {e}", exitCode: -1); }

            options.Remove("loglevel");
        }

        if (options.Count() != 0)
            PrintUsage(msg: $"Invalid option: {options.First().Value.Name}", exitCode: -1);

        if (remainingArgs.Count() == 0)
            PrintUsage(exitCode: -1);

        var command = remainingArgs.ElementAt(0);
        remainingArgs = remainingArgs.Skip(1);

        switch (command)
        {
            case "run": DoRun(remainingArgs); break;
            case "run-ir": DoRunIR(remainingArgs); break;
            case "make-tests": DoMakeTests(remainingArgs); break;
            default: PrintUsage("", msg: $"Unknown command: {command}", exitCode: -1); break;
        }
    }
}