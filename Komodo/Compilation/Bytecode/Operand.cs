using Komodo.Utilities;
using System.Text.RegularExpressions;

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
        public static readonly Regex PATTERN = new Regex("^\\$local(?<index>(0|([1-9][0-9]*)))$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol($"$local{Index}");

        public static Local Deserialize(SExpression sexpr)
        {
            var value = sexpr.ExpectUnquotedSymbol().ExpectValue(PATTERN).Value.Substring(6);

            if (UInt64.TryParse(value, out var result))
                return new Local(result);

            throw new SExpression.FormatException($"'{value}' is not a valid index", sexpr);
        }
    }

    public record Arg(UInt64 Index) : Operand, Source
    {
        public static readonly Regex PATTERN = new Regex("^\\$arg(?<index>(0|([1-9][0-9]*)))$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol($"$arg{Index}");

        public static Arg Deserialize(SExpression sexpr)
        {
            var value = sexpr.ExpectUnquotedSymbol().ExpectValue(PATTERN).Value.Substring(4);

            if (UInt64.TryParse(value, out var result))
                return new Arg(result);

            throw new SExpression.FormatException($"'{value}' is not a valid index", sexpr);
        }
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

    public record List(IEnumerable<IOperand> Operands) : Operand
    {
        public override SExpression AsSExpression() => new SExpression.List(Operands.Select(o => o.AsSExpression()));

        public static List Deserialize(SExpression sexpr, Func<SExpression, IOperand> deserializer) => new List(sexpr.ExpectList().Select(deserializer));
        public static List Deserialize(SExpression sexpr, Func<SExpression, int, IOperand> deserializer) => new List(sexpr.ExpectList().Select(deserializer));
    }

    public record Arguments(IEnumerable<Source> Sources) : Operand, IEnumerable<Source>
    {
        public override SExpression AsSExpression()
            => new SExpression.List(Sources.Select(s => s.AsSExpression()).Prepend(new SExpression.UnquotedSymbol("args")));

        public IEnumerator<Source> GetEnumerator() => Sources.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public Source this[int idx] => Sources.ElementAt(idx);

        public static Arguments Deserialize(SExpression sexpr) => new Arguments(sexpr
            .ExpectList()
            .ExpectLength(2, null).ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("args"))
            .Skip(1)
            .Select(Operand.DeserializeSource)
        );
    }

    public record Returns(IEnumerable<Destination> Destinations) : Operand, IEnumerable<Destination>
    {
        public override SExpression AsSExpression()
            => new SExpression.List(Destinations.Select(d => d.AsSExpression()).Prepend(new SExpression.UnquotedSymbol("returns")));

        public IEnumerator<Destination> GetEnumerator() => Destinations.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public Destination this[int idx] => Destinations.ElementAt(idx);

        public static Returns Deserialize(SExpression sexpr) => new Returns(sexpr
            .ExpectList()
            .ExpectLength(2, null).ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("returns"))
            .Skip(1)
            .Select(Operand.DeserializeDestination)
        );
    }

    public static Source DeserializeSource(SExpression sexpr)
    {
        try { return Constant.Deserialize(sexpr); }
        catch { }

        try { return Local.Deserialize(sexpr); }
        catch { }

        try { return Arg.Deserialize(sexpr); }
        catch { }

        try { return Stack.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid source operand: {sexpr}", sexpr);
    }

    public static Destination DeserializeDestination(SExpression sexpr)
    {
        try { return Local.Deserialize(sexpr); }
        catch { }

        try { return Stack.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid source operand: {sexpr}", sexpr);
    }
}