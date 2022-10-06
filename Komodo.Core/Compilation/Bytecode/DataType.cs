using System.Text.RegularExpressions;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record DataType
{
    public abstract uint ByteSize { get; }

    public abstract SExpression AsSExpression();

    public sealed override string ToString() => AsSExpression().ToString();

    public record I8 : DataType
    {
        public override uint ByteSize => 1;

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("I8");

        new public static I8 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("I8");
            return new I8();
        }
    }

    public record UI8 : DataType
    {
        public override uint ByteSize => 1;

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("UI8");

        new public static UI8 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("UI8");
            return new UI8();
        }
    }

    public record I64 : DataType
    {
        public override uint ByteSize => 8;

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("I64");

        new public static I64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("I64");
            return new I64();
        }
    }

    public record UI64 : DataType
    {
        public override uint ByteSize => 8;

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("UI64");

        new public static UI64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("UI64");
            return new UI64();
        }
    }

    public record Bool : DataType
    {
        public override uint ByteSize => 1;

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("Bool");

        new public static Bool Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("Bool");
            return new Bool();
        }
    }

    public record Array(DataType ElementType) : DataType
    {
        public override uint ByteSize => 16; // Length = 8, Address = 8

        public override SExpression AsSExpression() => new SExpression.List(new[]{
            new SExpression.UnquotedSymbol("Array"),
            ElementType.AsSExpression()
        });

        new public static Array Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("Array"))
                 .ExpectItem(1, DataType.Deserialize, out var elementType);

            return new Array(elementType);
        }
    }

    public T As<T>() where T : DataType => this as T ?? throw new Exception($"Value is not a {typeof(T)}.");

    public static DataType Deserialize(SExpression sexpr)
    {
        try { return I8.Deserialize(sexpr); }
        catch { }

        try { return UI8.Deserialize(sexpr); }
        catch { }

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