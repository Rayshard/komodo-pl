using Komodo.Core.Utilities;
using System.Text.RegularExpressions;

namespace Komodo.Core.Compilation.Bytecode;

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

    public record DataType(Bytecode.DataType Value) : Operand
    {
        public override SExpression AsSExpression() => Value.AsSExpression();

        public static DataType Deserialize(SExpression sexpr) => new DataType(Bytecode.DataType.Deserialize(sexpr));
    }

    public record Enumeration<T>(T Value) : Operand where T : struct, Enum
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Value.ToString());

        public static Enumeration<T> Deserialize(SExpression sexpr) => new Enumeration<T>(sexpr.ExpectEnum<T>());
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

    public record Array(Bytecode.DataType ElementType, IList<Source> Elements) : Operand, Source
    {
        public override SExpression AsSExpression() => new SExpression.List(new []
        {
            new Bytecode.DataType.Array(ElementType).AsSExpression(),
            new SExpression.List(Elements.Select(element => element.AsSExpression()))
        });

        public static Array Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2, null);

            if (list.Count() == 2)
            {
                try
                {
                    var elementType = list.Expect(Bytecode.DataType.Array.Deserialize).ElementType;
                    return new Array(elementType, new Source[0]);
                }
                catch
                {
                    var elementType = list[0].Expect(Bytecode.DataType.Array.Deserialize).ElementType;
                    var elements = list[1].ExpectList().Select(DeserializeSource).ToList();

                    return new Array(elementType, elements);
                }
            }
            else
            {
                list[0].ExpectUnquotedSymbol().ExpectValue("Array");

                var elementType = list[1].Expect(Bytecode.DataType.Deserialize);
                var elements = list.Skip(2).Select(DeserializeSource).ToArray();

                return new Array(elementType, elements);
            }
        }
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

        try { return Array.Deserialize(sexpr); }
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