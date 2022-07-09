using Komodo.Compilation.CST;
using Komodo.Utilities;

namespace Komodo.Interpretation;

public interface IResult { }

public interface IKomodoValue : IResult { }

public record KomodoValue<T>(T Value) : IKomodoValue;

public record KomodoException(string Message, TextLocation Location) : IResult
{
    public override string ToString() => $"Exception at {Location}: {Message}";

    public void Print(Dictionary<string, TextSource> sources)
    {
        var source = sources[Location.SourceName];

        Console.WriteLine($"[Exception at {source.Name}:{source.GetPosition(Location.Start)}] {Message}");
    }

    public static KomodoException DivisionByZero(TextLocation location) => new KomodoException("Division by zero", location);
    public static KomodoException VariableRedefinition(Token token) => new KomodoException("Variable has aleady been defined", token.Location);
    public static KomodoException UnknownVariable(Token token) => new KomodoException($"Variable '{token.Value}' was never defined", token.Location);
}

public class Interpreter
{
    private Dictionary<string, IKomodoValue> environment;

    public Interpreter()
    {
        environment = new Dictionary<string, IKomodoValue>();
    }

    public void PrintEnvironment()
    {
        Console.WriteLine("========== Environment ==========");

        foreach (var (id, value) in environment)
            Console.WriteLine($" -> {id}: {value}");


        Console.WriteLine("============== End ==============");
    }

    public IResult Evaluate(INode node) => node switch
    {
        Literal l => Evaluate(l),
        BinopExpression b => Evaluate(b),
        ParenthesizedExpression p => Evaluate(p.Expression),
        IdentifierExpression i => Evaluate(i),
        VariableDeclaration vd => Evaluate(vd),
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

    private IResult Evaluate(IdentifierExpression node)
    {
        if (environment.ContainsKey(node.ID.Value)) { return environment[node.ID.Value]; }
        else { return KomodoException.UnknownVariable(node.ID); }
    }

    private IResult Evaluate(VariableDeclaration node)
    {
        var value = Evaluate(node.Expression);
        if (value is KomodoException) { return value; }
        else if (value is not IKomodoValue) { throw new Exception($"Expected KomodoValue but got {value}"); }
        else if (environment.ContainsKey(node.Identifier.Value)) { return KomodoException.VariableRedefinition(node.Identifier); }

        environment.Add(node.Identifier.Value, (IKomodoValue)value);
        return value;
    }
}