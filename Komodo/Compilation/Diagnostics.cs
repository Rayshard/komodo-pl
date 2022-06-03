using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Komodo.Utilities
{
    public enum DiagnosticType { Warning, Error };

    public class Diagnostic
    {
        public DiagnosticType Type { get; }
        public Location Location { get; }
        public string Message { get; }

        public Diagnostic(DiagnosticType type, Location location, string message)
        {
            Type = type;
            Location = location;
            Message = message;
        }

        public override string ToString() => $"{Type} {Location}: {Message}";
    }

    public class Diagnostics
    {
        List<Diagnostic> _errors = new List<Diagnostic>();
        List<Diagnostic> _warnings = new List<Diagnostic>();

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
    }
}