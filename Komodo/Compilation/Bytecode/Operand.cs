using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public interface IOperand
{
    public SExpression AsSExpression();
}

public abstract record Operand : IOperand
{
    public abstract SExpression AsSExpression();

    public interface Source : IOperand { }
    public interface Destination : IOperand { }

    public record Constant(Value Value) : Operand, Source
    {
        public override SExpression AsSExpression() => Value.AsSExpression();

        public static Constant Deserialize(SExpression sexpr) => new Constant(Value.Deserialize(sexpr));
    }

    public record Enumeration<T>(T Value) : Operand where T : struct, Enum
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Value.ToString());

        public static Enumeration<T> Deserialize(SExpression sexpr) => new Enumeration<T>(sexpr.AsEnum<T>());
    }

    public record Identifier(string Value) : Operand
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Value);

        public static Identifier Deserialize(SExpression sexpr) => new Identifier(sexpr.ExpectUnquotedSymbol().Value);
    }

    public record Local(UInt64 Index) : Operand, Source, Destination
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol($"$local{Index}");

        public static Local Deserialize(SExpression sexpr) => new Local(sexpr.AsUInt64());
    }

    public record Arg(UInt64 Index) : Operand, Source, Destination
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol($"$arg{Index}");

        public static Arg Deserialize(SExpression sexpr) => new Arg(sexpr.AsUInt64());
    }

    public record Stack : Operand, Source, Destination
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("$stack");

        public static Stack Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("$stack");
            return new Stack();
        }
    }

    public static Source DeserializeSource(SExpression sexpr) => Constant.Deserialize(sexpr);
}