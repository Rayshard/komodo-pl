using Komodo.Core.Compilation.Bytecode.Transpilers;
using Komodo.Core.Interpretation;
using Komodo.Core.Utilities;

namespace Komodo.Core;


public static class Commands
{
    public static string? TranspileIR(TextSource source, TranspilationTarget target)
    {
        try
        {
            var sexpr = SExpression.Parse(new TextSourceReader(source));
            var program = Core.Compilation.Bytecode.Program.Deserialize(sexpr);

            switch (target)
            {
                case TranspilationTarget.CPP: return new CPPTranspiler().Convert(program);
                default: Logger.Error($"Transpilation to '{target}' is not supported."); break;
            }
        }
        catch (SExpression.ParseException e) { Logger.Error($"{source.GetTerminalLink(e.Location.Start)} {e.Message}"); }
        catch (SExpression.FormatException e) { Logger.Error($"{source.GetTerminalLink(e.Location!.Start)} {e.Message}"); }
        catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }

        return null;
    }

    public static int? RunIR(TextSource source, InterpreterConfig interpreterConfig)
    {
        var sources = new Dictionary<string, TextSource>();
        sources.Add(source.Name, source);

        try
        {
            var sexpr = SExpression.Parse(new TextSourceReader(source));
            var program = Compilation.Bytecode.Program.Deserialize(sexpr);
            
            return RunIR(program, interpreterConfig);
        }
        catch (SExpression.ParseException e) { Logger.Error($"{e.Location.ToTerminalLink(sources)} {e.Message}"); }
        catch (SExpression.FormatException e) { Logger.Error($"{e.Location!.ToTerminalLink(sources)} {e.Message}"); }
        catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }

        return null;
    }

    public static int RunIR(Compilation.Bytecode.Program program, InterpreterConfig interpreterConfig)
    {
        var interpreter = new Interpreter(program, interpreterConfig);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exitcode = (int)interpreter.Run();

        stopwatch.Stop();

        Logger.Info($"Exited with code {exitcode}");
        Logger.Info($"Finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");

        return exitcode;
    }

    public static string? FormatIR(TextSource source)
    {
        try
        {
            var sexpr = SExpression.Parse(new TextSourceReader(source));
            var program = Core.Compilation.Bytecode.Program.Deserialize(sexpr);
            var formatter = new Core.Compilation.Bytecode.Formatter();

            return formatter.Convert(program);
        }
        catch (SExpression.ParseException e) { Logger.Error($"{source.GetTerminalLink(e.Location.Start)} {e.Message}"); }
        catch (SExpression.FormatException e) { Logger.Error($"{source.GetTerminalLink(e.Location!.Start)} {e.Message}"); }
        catch (Exception e) { Logger.Error(e.Message + "\n" + e.StackTrace); }

        return null;
    }
}