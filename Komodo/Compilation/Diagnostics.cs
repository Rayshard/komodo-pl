using System.Collections.ObjectModel;

namespace Komodo.Utilities;

public record LineHint(TextLocation Location, string Message);

public enum DiagnosticType { Warning, Error };

public record Diagnostic(DiagnosticType Type, TextLocation Location, string Message, LineHint[]? Hints = null)
{
    public override string ToString() => $"{Type} {Location}: {Message}";

    public void Print(Dictionary<string, TextSource> sources)
    {
        var source = sources[Location.SourceName];

        // Print general message
        if (Type == DiagnosticType.Error) { Console.ForegroundColor = ConsoleColor.Red; }
        else if (Type == DiagnosticType.Warning) { Console.ForegroundColor = ConsoleColor.Yellow; }
        else { Console.ForegroundColor = ConsoleColor.Gray; }

        Console.WriteLine($"[{Type.ToString().ToUpper()}] {source.Name}:{source.GetPosition(Location.Start)} {Message}");
        Console.ResetColor();

        // Print hints
        if (Hints == null)
            return;

        var hintsSortedByLineNumber = Hints.OrderBy(h => h.Location.Start);

        foreach (var hint in hintsSortedByLineNumber)
        {
            var (start, end) = (source.GetPosition(hint.Location.Start), source.GetPosition(hint.Location.End));
            var preLineWidth = end.Line.ToString().Length + 7;

            for (int lineNumber = start.Line; lineNumber <= end.Line; lineNumber++)
            {
                // Print line number and text
                var line = source.Lines[lineNumber].Text;

                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write($"{lineNumber} |\t".PadLeft(preLineWidth));
                Console.ResetColor();
                Console.WriteLine(line);

                // Print hint
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("|\t".PadLeft(preLineWidth));

                if (start.Line == end.Line)
                {
                    Console.Write(new string(' ', start.Column - 1) + new string('^', end.Column - start.Column));
                    Console.WriteLine(" " + hint.Message);
                }
                else if (lineNumber == start.Line) { Console.WriteLine(new string('^', line.Length - start.Column)); }
                else if (lineNumber == end.Line)
                {
                    Console.Write(new string('^', end.Column - 1));
                    Console.WriteLine(" " + hint.Message);
                }
                else { Console.WriteLine(new string('^', line.Length)); }

                Console.ResetColor();
            }
        }
    }
}

public class Diagnostics
{
    List<Diagnostic> _errors, _warnings;

    public Diagnostics(IEnumerable<Diagnostic>? errors = null, IEnumerable<Diagnostic>? warnings = null)
    {
        _errors = errors?.ToList() ?? new List<Diagnostic>();
        _warnings = warnings?.ToList() ?? new List<Diagnostic>();
    }

    public void Add(Diagnostic d)
    {
        switch (d.Type)
        {
            case DiagnosticType.Error:
                _errors.Add(d);
                break;
            case DiagnosticType.Warning:
                _warnings.Add(d);
                break;
            default: throw new NotImplementedException(d.Type.ToString());
        }
    }

    public bool HasError => _errors.Count != 0;
    public bool HasWarning => _warnings.Count != 0;
    public bool Empty => !HasError && !HasWarning;

    public ReadOnlyCollection<Diagnostic> Errors => _errors.AsReadOnly();
    public ReadOnlyCollection<Diagnostic> Warnings => _warnings.AsReadOnly();

    public void Print(Dictionary<string, TextSource> sources)
    {
        foreach (var w in Warnings)
            w.Print(sources);

        foreach (var e in Errors)
            e.Print(sources);
    }

    public void Append(Diagnostics diagnostics)
    {
        _errors.AddRange(diagnostics._errors);
        _warnings.AddRange(diagnostics._warnings);
    }

    public static Diagnostics Aggregate(params Diagnostics[] diagnosticsArray)
    {
        var result = new Diagnostics();

        foreach (var diagnostics in diagnosticsArray)
            result.Append(diagnostics);

        return result;
    }
}
