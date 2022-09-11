namespace Komodo;

using System.Text.RegularExpressions;
using Komodo.Compilation;
using Komodo.Compilation.CST;
using Komodo.Interpretation;
using Komodo.Utilities;
using Newtonsoft.Json.Linq;

static class Entry
{
    static int DoRun(CLI.Arguments args)
    {
        var inputFilePath = args.Get<string>("file");
        var printTokens = args.Get<bool>("print-tokens");
        var printCST = args.Get<bool>("print-cst");

        var sourceMap = new Dictionary<string, TextSource>() { { "std", new TextSource("std", "") } };

        try
        {
            var source = TextSource.Load(inputFilePath);
            sourceMap.Add(source.Name, source);

            var module = Compiler.Compile(sourceMap, inputFilePath, printTokens, printCST);
            if (module is null)
            {
                Logger.Error("Compilation was unsuccessful. Check diagnostics for further information.");
                return -1;
            }

            return 0;
        }
        catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }

        return -1;
    }

    static int DoRunIR(CLI.Arguments args)
    {
        var inputFilePath = args.Get<string>("file");

        if (!File.Exists(inputFilePath))
        {
            Logger.Error($"File does not exist at {inputFilePath}");
            return -1;
        }
        else if (!inputFilePath.EndsWith(".kmdir"))
        {
            Logger.Error("Expected a komodo ir file (a file ending in .kmdir)");
            return -1;
        }

        var sources = new Dictionary<string, TextSource>();
        sources.Add(inputFilePath, new TextSource(inputFilePath, File.ReadAllText(inputFilePath)));

        try
        {
            var sexpr = SExpression.Parse(new TextSourceReader(sources[inputFilePath]));
            var program = Compilation.Bytecode.Program.Deserialize(sexpr);
            var interpreter = new Interpreter(program);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var exitcode = interpreter.Run();
            stopwatch.Stop();

            Logger.Info($"Exited with code {exitcode}");
            Logger.Info($"Finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");

            return (int)exitcode;
        }
        catch (SExpression.ParseException e) { Logger.Error($"{e.Location.ToTerminalLink(sources)} {e.Message}"); }
        catch (SExpression.FormatException e) { Logger.Error($"{e.Location!.ToTerminalLink(sources)} {e.Message}"); }
        catch (InterpreterException e)
        {
            Logger.Error(e.Message);
            Logger.Debug(e.BaseException.StackTrace ?? "No Stack Trace", true);
        }
        catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }

        return -1;
    }

    static int DoFormat(CLI.Arguments args)
    {
        var expectedFileType = args.Get<string>("type");
        var inputFilePath = args.Get<string>("file");

        if (!File.Exists(inputFilePath))
        {
            Logger.Error($"File does not exist at {inputFilePath}");
            return -1;
        }

        var source = new TextSource(inputFilePath, File.ReadAllText(inputFilePath));

        switch (expectedFileType)
        {
            case "kmdir":
                {
                    try
                    {
                        var sexpr = SExpression.Parse(new TextSourceReader(source));
                        var program = Compilation.Bytecode.Program.Deserialize(sexpr);

                        Console.WriteLine(Compilation.Bytecode.Formatter.Format(program));
                        return 0;
                    }
                    catch (SExpression.ParseException e) { Logger.Error($"{source.GetTerminalLink(e.Location.Start)} {e.Message}"); }
                    catch (SExpression.FormatException e) { Logger.Error($"{source.GetTerminalLink(e.Location!.Start)} {e.Message}"); }
                    catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }
                }
                break;
            default: Logger.Error($"Formatting for '{expectedFileType}' files is not supported."); break;
        }

        return -1;
    }

    // static void DoMakeTests(IEnumerable<string> args)
    // {
    //     if (args.Count() != 1)
    //         PrintUsage("make-tests", msg: "Expected an output directory", exitCode: -1);

    //     var outputDirectory = args.ElementAt(0);

    //     if (!Directory.Exists(outputDirectory))
    //     {
    //         Console.WriteLine($"Specified directory {outputDirectory} is not valid or does not exist!");
    //         Environment.Exit(1);
    //     }

    //     // Parser Tests
    //     var parserTestCases = new Dictionary<string, (string, string, Func<TokenStream, Diagnostics?, INode?>)>();
    //     parserTestCases.Add("expr-int-literal", ("123", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
    //     parserTestCases.Add("expr-binop", ("1 * 4 - 7 / 6 + 9", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
    //     parserTestCases.Add("parenthesized-expression", ("(123 + (456 - 789))", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
    //     parserTestCases.Add("identifier", ("abc", "ParseExpression", (stream, diagnostics) => Parser.ParseExpression(stream, diagnostics)));
    //     parserTestCases.Add("variable-declaration", ("var x = 123;", "ParseStatement", (stream, diagnostics) => Parser.ParseStatement(stream, diagnostics)));

    //     foreach (var (name, (input, functionName, function)) in parserTestCases)
    //     {
    //         var filePath = Path.Join(outputDirectory, $"{name}.json");

    //         if (File.Exists(filePath))
    //         {
    //             Logger.Info($"Test Case '{name}' already exists.");
    //             continue;
    //         }

    //         var source = new TextSource("test", input);
    //         var tokenStream = Lexer.Lex(source);

    //         var outputDiagnostics = new Diagnostics();
    //         var outputCSTNode = function(tokenStream, outputDiagnostics) ?? throw new Exception($"Could not parse cst node for test case: {name}");

    //         if (!outputDiagnostics.Empty)
    //         {
    //             Logger.Info($"Test Case \"{name}\" has unexpected diagnostics:");
    //             outputDiagnostics.Print(new Dictionary<string, TextSource>(new[] { new KeyValuePair<string, TextSource>(source.Name, source) }));
    //             continue;
    //         }

    //         var outputJson = new JObject();
    //         outputJson.Add("input", input);
    //         outputJson.Add("function", functionName);
    //         outputJson.Add("expected", JToken.Parse(JsonSerializer.Serialize(outputCSTNode).ToJsonString()));

    //         File.WriteAllText(filePath, outputJson.ToString());
    //         Logger.Info($"Created test case '{name}'.");
    //     }
    // }

    static int Main(string[] args)
    {
        Logger.MinLevel = LogLevel.ERROR;

        var formatCommand = new CLI.Command(
            "format",
            "Reads the input file and prints the formatted version to standard output.",
            new CLI.Parameter[] {
                new CLI.Parameter.Option("type", value => value, "the type of formatting the perform"),
                new CLI.Parameter.Positional("file", value => value, "the input file to format")
            },
            new CLI.Command[] { },
            DoFormat
        );

        var runIRCommand = new CLI.Command(
            "run-ir",
            "Executes the input ir file.",
            new CLI.Parameter[] {
                new CLI.Parameter.Positional("file", value => value, "the input file to run")
            },
            new CLI.Command[] { },
            DoRunIR
        );

        var runCommand = new CLI.Command(
            "run",
            "Executes the input Komodo file.",
            new CLI.Parameter[] {
                new CLI.Parameter.Boolean("print-tokens", "print the parsed file's tokens to standard output"),
                new CLI.Parameter.Boolean("print-cst", "print the parsed file's concrete syntax tree to standard output"),
                new CLI.Parameter.Positional("file", value => value, "the input file to run")
            },
            new CLI.Command[] { },
            DoRun
        );

        var kmdCommand = new CLI.Command(
            "kmd",
            "Execute a command for the Komodo compiler.",
            new CLI.Parameter[] {
                new CLI.Parameter.Option("loglevel", value => Enum.Parse<LogLevel>(value), "the global logging level for the compiler", LogLevel.ERROR)
            },
            new CLI.Command[] { runCommand, formatCommand, runIRCommand },
            arguments =>
            {
                Logger.MinLevel = arguments.Get<LogLevel>("loglevel");
                return 0;
            }
        );

        return kmdCommand.Run(args);
    }
}