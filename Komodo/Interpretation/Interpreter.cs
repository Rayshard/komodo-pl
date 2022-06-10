using Komodo.Compilation.ConcreteSyntaxTree;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public interface IResult { }

public record KomodoValue<T>(T Value) : IResult;

public record KomodoException(string Message, Location Location, IEnumerable<Hint> Hints) : IResult
{
    public static KomodoException DivisionByZero(Location location) => new KomodoException("Division by zero", location, new Hint[] { new Hint(location, "this expression evaluated to 0") });
    public static KomodoException InvalidOperation(Location location) => new KomodoException("Invalid operation", location, new Hint[] { new Hint(location) });

    public void Print(Dictionary<string, SourceFile> sourceFiles)
    {
        Console.WriteLine($"Exception: {Message}");
        Console.WriteLine($"    at {Location}");

        SourceFile sf = sourceFiles[Location.SourceFileName];
        var (startLine, endLine) = (Location.Span.Start.Line, Location.Span.End.Line);
        var lines = sf.Text.Split('\n').Skip(startLine - 1).Take(endLine - startLine + 1).Select((line, index) => (startLine + index, line, Hints.Where(h => h.Line == startLine)));
        var preLineWidth = endLine.ToString().Length + 7;

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine("|\t".PadLeft(preLineWidth));
        Console.ResetColor();

        foreach (var (lineNumber, lineText, hints) in lines)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write($"{lineNumber} |\t".PadLeft(preLineWidth));
            Console.ResetColor();
            Console.WriteLine(lineText);

            // Print hints
            foreach (var hint in hints)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("|\t".PadLeft(preLineWidth));
                Console.WriteLine(hint);
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine("|\t".PadLeft(preLineWidth));
        Console.ResetColor();
    }
}

public class Interpreter
{
    public Interpreter()
    {

    }

    public IResult Evaluate(ICSTNode node) => node switch
    {
        CSTLiteral l => Evaluate(l),
        CSTBinopExpression b => Evaluate(b),
        CSTParenthesizedExpression p => Evaluate(p.Expression),
        _ => throw new NotImplementedException(node.NodeType.ToString())
    };

    private IResult Evaluate(CSTLiteral node)
    {
        switch (node.LiteralType)
        {
            case LiteralType.Int: return new KomodoValue<int>(int.Parse(node.Token.Value));
            default: throw new NotImplementedException(node.LiteralType.ToString());
        }
    }

    private IResult Evaluate(CSTBinopExpression node)
    {
        var leftResult = Evaluate(node.Left);
        var rightResult = Evaluate(node.Right);

        switch ((leftResult, node.Op.Operation, rightResult))
        {
            case (KomodoException exception, _, _): return exception;
            case (_, _, KomodoException exception): return exception;
            case (KomodoValue<int> l, BinaryOperation.Add, KomodoValue<int> r): return new KomodoValue<int>(l.Value + r.Value);
            case (KomodoValue<int> l, BinaryOperation.Sub, KomodoValue<int> r): return new KomodoValue<int>(l.Value - r.Value);
            case (KomodoValue<int> l, BinaryOperation.Multiply, KomodoValue<int> r): return new KomodoValue<int>(l.Value * r.Value);
            case (KomodoValue<int> l, BinaryOperation.Divide, KomodoValue<int> r):
                {
                    if (r.Value == 0) { return KomodoException.DivisionByZero(node.Right.Location); }
                    else { return new KomodoValue<int>(l.Value / r.Value); }
                }
            default: return KomodoException.InvalidOperation(node.Op.Location);
        }
    }
}