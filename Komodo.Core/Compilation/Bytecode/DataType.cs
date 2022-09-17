using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record DataType
{
    public abstract SExpression AsSExpression();

    public record I64 : DataType
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("I64");

        public override string ToString() => AsSExpression().ToString();

        new public static I64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("I64");
            return new I64();
        }
    }

    public record UI64 : DataType
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("UI64");

        public override string ToString() => AsSExpression().ToString();

        new public static UI64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("UI64");
            return new UI64();
        }
    }

    public record Bool : DataType
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("Bool");

        public override string ToString() => AsSExpression().ToString();

        new public static Bool Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("Bool");
            return new Bool();
        }
    }

    public record Array(DataType ElementType) : DataType
    {
        public override SExpression AsSExpression() => new SExpression.List(new[]{
            new SExpression.UnquotedSymbol("Array"),
            ElementType.AsSExpression()
        });

        public override string ToString() => AsSExpression().ToString();

        new public static Array Deserialize(SExpression sexpr)
        {
            var list = sexpr.ExpectList().ExpectLength(2);
            list[0].ExpectUnquotedSymbol().ExpectValue("Array");

            return new Array(DataType.Deserialize(list[1]));
        }
    }

    public T As<T>() where T : DataType => this as T ?? throw new Exception($"Value is not a {typeof(T)}.");

    public static DataType Deserialize(SExpression sexpr)
    {
        try { return I64.Deserialize(sexpr); }
        catch { }

        try { return UI64.Deserialize(sexpr); }
        catch { }

        try { return Bool.Deserialize(sexpr); }
        catch { }

        try { return Array.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid data type: {sexpr}", sexpr);
    }
}