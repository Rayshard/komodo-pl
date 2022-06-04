namespace Komodo.Utilities;

using System.Collections.ObjectModel;
using System.Diagnostics;

public enum DiagnosticType { Warning, Error };

public record Diagnostic(DiagnosticType Type, Location Location, string Message)
{
    public override string ToString() => $"{Type} {Location}: {Message}";
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
            default:
                Trace.Fail("Unimplemented");
                break;
        }
    }

    public bool HasError => _errors.Count != 0;
    public bool HasWarning => _warnings.Count != 0;
    public bool Empty => !HasError && !HasWarning;

    public ReadOnlyCollection<Diagnostic> Errors => _errors.AsReadOnly();
    public ReadOnlyCollection<Diagnostic> Warnings => _warnings.AsReadOnly();

    public void Print()
    {
        if (HasError)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(String.Join('\n', _errors));
            Console.ResetColor();
        }

        if (HasWarning)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(String.Join('\n', _warnings));
            Console.ResetColor();
        }
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
