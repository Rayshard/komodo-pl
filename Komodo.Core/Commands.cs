using Komodo.Interpretation;
using Komodo.Utilities;

namespace Komodo;

public static class Commands
{
    public static int RunIR(TextSource source, InterpreterConfig interpreterConfig)
    {
        var sources = new Dictionary<string, TextSource>();
        sources.Add(source.Name, source);

        try
        {
            var sexpr = SExpression.Parse(new TextSourceReader(source));
            var program = Compilation.Bytecode.Program.Deserialize(sexpr);
            var interpreter = new Interpreter(program, interpreterConfig);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var exitcode = interpreter.Run();
            stopwatch.Stop();

            Logger.Info($"Exited with code {exitcode}");
            Logger.Info($"Finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");

            return (int)exitcode;
        }
        catch (SExpression.ParseException e) { Logger.Error($"{e.Location.ToTerminalLink(sources)} {e.Message}"); }
        catch (SExpression.FormatException e) { Logger.Error($"{e.Location!.ToTerminalLink(sources)} {e.Message}"); }
        catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }

        return -1;
    }
}