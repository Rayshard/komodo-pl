using Komodo.Compilation.CST;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public interface IResult { }

public record KomodoValue<T>(T Value) : IResult;

public record KomodoException(string Message, TextLocation Location, IEnumerable<Hint> Hints) : IResult
{
    public static KomodoException DivisionByZero(TextLocation location) => new KomodoException("Division by zero", location, new Hint[] { new Hint(location.Start, location.End, "this expression evaluated to 0") });
    public static KomodoException InvalidOperation(TextLocation location) => new KomodoException("Invalid operation", location, new Hint[] { new Hint(location.Start, location.End) });

    public void Print(Dictionary<string, TextSource> sources)
    {
        Console.WriteLine($"Exception: {Message}");
        Console.WriteLine($"    at {Location}");

        var source = sources[Location.SourceName];
        var (start, end) = (source.GetPosition(Location.Start), source.GetPosition(Location.End));
        var lines = source.Text.Split('\n').Skip(start.Line - 1).Take(end.Line - start.Line + 1).Select((line, index) => (start.Line + index, line, Hints.Where(h => source.GetPosition(h.Start).Line == start.Line)));
        var preLineWidth = end.Line.ToString().Length + 7;

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

    public IResult Evaluate(INode node) => node switch
    {
        Literal l => Evaluate(l),
        BinopExpression b => Evaluate(b),
        ParenthesizedExpression p => Evaluate(p.Expression),
        _ => throw new NotImplementedException(node.NodeType.ToString())
    };

    private IResult Evaluate(Literal node)
    {
        switch (node.LiteralType)
        {
            case LiteralType.Int: return new KomodoValue<int>(int.Parse(node.Token.Value));
            default: throw new NotImplementedException(node.LiteralType.ToString());
        }
    }

    private IResult Evaluate(BinopExpression node)
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