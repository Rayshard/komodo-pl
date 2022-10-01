using System.Text.RegularExpressions;
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

    public record Char : DataType
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("Char");

        public override string ToString() => AsSExpression().ToString();

        new public static Char Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("Char");
            return new Char();
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

        try { return Char.Deserialize(sexpr); }
        catch { }

        try { return Array.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid data type: {sexpr}", sexpr);
    }
}

public record NamedDataType(DataType DataType, string Name)
{
    public KeyValuePair<string, DataType> AsKeyValuePair() => KeyValuePair.Create<string, DataType>(Name, DataType);

    public static NamedDataType Deserialize(SExpression sexpr, Regex? nameRegex = null)
    {
        var list = sexpr.ExpectList().ExpectLength(2);
        var nameNode = list[1].ExpectUnquotedSymbol();

        return new NamedDataType(DataType.Deserialize(list[0]), nameRegex is null ? nameNode.Value : nameNode.ExpectValue(nameRegex).Value);
    }
}

public record OptionallyNamedDataType(DataType DataType, string? Name = null)
{
    public NamedDataType ToNamed() => Name is not null ? new NamedDataType(DataType, Name) : throw new InvalidOperationException("Name is null!");

    public static OptionallyNamedDataType Deserialize(SExpression sexpr, Regex? nameRegex = null)
    {
        try { return new OptionallyNamedDataType(DataType.Deserialize(sexpr)); }
        catch { }

        try
        {
            var named = NamedDataType.Deserialize(sexpr);
            return new OptionallyNamedDataType(named.DataType, named.Name);
        }
        catch { }

        throw new SExpression.FormatException($"Invalid named data type: {sexpr}", sexpr);
    }
}