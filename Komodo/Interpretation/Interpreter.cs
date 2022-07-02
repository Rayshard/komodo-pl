using Komodo.Compilation.CST;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public interface IResult { }

public record KomodoValue<T>(T Value) : IResult;

public record KomodoException(string Message, TextLocation Location) : IResult
{
    public override string ToString() => $"Exception at {Location}: {Message}";

    public static KomodoException DivisionByZero(TextLocation location) => new KomodoException("Division by zero", location);
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
            default: throw new NotImplementedException((leftResult.GetType(), node.Op.Operation, rightResult.GetType()).ToString());
        }
    }
}