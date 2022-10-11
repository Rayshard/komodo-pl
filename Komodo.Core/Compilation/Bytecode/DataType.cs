using System.Text.RegularExpressions;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public abstract record DataType
{
    public abstract UInt64 ByteSize { get; }

    public abstract SExpression AsSExpression();
    public abstract string AsMangledString();

    public sealed override string ToString() => AsSExpression().ToString();

    public abstract record Primitive : DataType;
    
    public abstract record Pointer : DataType
    {
        public sealed override UInt64 ByteSize => ByteSizeOf<Pointer>();
    }

    public record I8 : Primitive
    {
        private static string Symbol => "I8";

        public override UInt64 ByteSize => ByteSizeOf<I8>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static I8 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new I8();
        }
    }

    public record UI8 : Primitive
    {
        private static string Symbol => "UI8";

        public override UInt64 ByteSize => ByteSizeOf<UI8>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static UI8 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new UI8();
        }
    }

    public record I16 : Primitive
    {
        private static string Symbol => "I16";

        public override UInt64 ByteSize => ByteSizeOf<I16>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static I16 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new I16();
        }
    }

    public record UI16 : Primitive
    {
        private static string Symbol => "UI16";

        public override UInt64 ByteSize => ByteSizeOf<UI16>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static UI16 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new UI16();
        }
    }

    public record I32 : Primitive
    {
        private static string Symbol => "I32";

        public override UInt64 ByteSize => ByteSizeOf<I32>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static I32 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new I32();
        }
    }

    public record UI32 : Primitive
    {
        private static string Symbol => "UI32";

        public override UInt64 ByteSize => ByteSizeOf<UI32>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static UI32 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new UI32();
        }
    }

    public record I64 : Primitive
    {
        private static string Symbol => "I64";

        public override UInt64 ByteSize => ByteSizeOf<I64>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static I64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new I64();
        }
    }

    public record UI64 : Primitive
    {
        private static string Symbol => "UI64";

        public override UInt64 ByteSize => ByteSizeOf<UI64>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static UI64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new UI64();
        }
    }

    public record F32 : Primitive
    {
        private static string Symbol => "F32";

        public override UInt64 ByteSize => ByteSizeOf<F32>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static F32 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new F32();
        }
    }

    public record F64 : Primitive
    {
        private static string Symbol => "F64";

        public override UInt64 ByteSize => ByteSizeOf<F64>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Symbol);
        public override string AsMangledString() => Symbol;

        new public static F64 Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue(Symbol);
            return new F64();
        }
    }

    public record Bool : Primitive
    {
        public override UInt64 ByteSize => ByteSizeOf<Bool>();

        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("Bool");
        public override string AsMangledString() => "Bool";

        new public static Bool Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("Bool");
            return new Bool();
        }
    }
    
    public record Array(DataType ElementType) : Pointer
    {
        public override SExpression AsSExpression() => new SExpression.List(new[]{
            new SExpression.UnquotedSymbol("Array"),
            ElementType.AsSExpression()
        });
        public override string AsMangledString() => $"{ElementType.AsMangledString()}[]";

        new public static Array Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("Array"))
                 .ExpectItem(1, DataType.Deserialize, out var elementType);

            return new Array(elementType);
        }
    }

    public record Type : Pointer
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("Type");
        public override string AsMangledString() => "Type";

        new public static Type Deserialize(SExpression sexpr)
        {
            sexpr.ExpectUnquotedSymbol().ExpectValue("Type");
            return new Type();
        }
    }

    public record Reference(DataType ValueType) : Pointer
    {
        public override SExpression AsSExpression() => new SExpression.List(new[] { new SExpression.UnquotedSymbol("Ref"), ValueType.AsSExpression() });

        public override string AsMangledString() => $"{ValueType.AsMangledString()}@";

        new public static Reference Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("Ref"))
                 .ExpectItem(1, DataType.Deserialize, out var valueType);

            return new Reference(valueType);
        }
    }

    public record Function(VSROCollection<DataType> Parameters, VSROCollection<DataType> Returns) : Pointer
    {
        public override SExpression AsSExpression() => new SExpression.List(new SExpression[] {
            new SExpression.UnquotedSymbol("Func"),
            new SExpression.List(Parameters.Select(p => p.AsSExpression())),
            new SExpression.List(Returns.Select(r => r.AsSExpression())),
        });

        public override string AsMangledString() => $"({Parameters.Select(p => p.AsMangledString()).Stringify(", ")}) -> ({Returns.Select(p => p.AsMangledString()).Stringify(", ")})";

        new public static Function Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(3)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("Func"))
                 .ExpectItem(1, item => item.ExpectList(), out var paramsList)
                 .ExpectItem(2, item => item.ExpectList(), out var returnsList);

            paramsList.ExpectItems(DataType.Deserialize, out var parameters);
            returnsList.ExpectItems(DataType.Deserialize, out var returns);

            return new Function(parameters.ToVSROCollection(), returns.ToVSROCollection());
        }
    }

    public T As<T>() where T : DataType => this as T ?? throw new Exception($"Value is not a {typeof(T)}.");

    public static UInt64 ByteSizeOf<T>() where T : DataType => typeof(T) switch
    {
        var type when type == typeof(I8) || type == typeof(UI8) || type == typeof(Bool) => 1,
        var type when type == typeof(I16) || type == typeof(UI16) => 2,
        var type when type == typeof(I32) || type == typeof(UI32) || type == typeof(F32) => 4,
        var type when type == typeof(I64) || type == typeof(UI64) || type == typeof(F64) => 8,
        var type when type == typeof(Pointer) || type.IsSubclassOf(typeof(Pointer)) => 8,
        var type => throw new NotImplementedException(type.ToString())
    };

    private static Func<SExpression, DataType>[] Deserializers => new Func<SExpression, DataType>[] {
        I8.Deserialize,
        UI8.Deserialize,
        I16.Deserialize,
        UI16.Deserialize,
        I32.Deserialize,
        UI32.Deserialize,
        I64.Deserialize,
        UI64.Deserialize,
        F32.Deserialize,
        F64.Deserialize,
        Bool.Deserialize,
        Array.Deserialize,
        Type.Deserialize,
        Reference.Deserialize,
        Function.Deserialize,
    };

    public static DataType Deserialize(SExpression sexpr)
    {
        foreach (var deserializer in Deserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid data type: {sexpr}", sexpr);
    }

    public static DataType Demangle(string mangledString) => mangledString switch
    {
        "I8" => new I8(),
        "UI8" => new UI8(),
        "I16" => new I16(),
        "UI16" => new UI16(),
        "I32" => new I32(),
        "UI32" => new UI32(),
        "I64" => new I64(),
        "UI64" => new UI64(),
        "F32" => new F32(),
        "F64" => new F64(),
        "Bool" => new Bool(),
        "Type" => new Type(),
        var ms when ms.EndsWith("@") => new Reference(Demangle(ms.Substring(0, ms.Length - 1))),
        var ms when ms.EndsWith("[]") => new Array(Demangle(ms.Substring(0, ms.Length - 2))),
        var ms => throw new Exception($"Unable to parse mangled datatype string: {ms}")
    };
}

public record NamedDataType(DataType DataType, string Name)
{
    public KeyValuePair<string, DataType> AsKeyValuePair() => KeyValuePair.Create<string, DataType>(Name, DataType);

    public SExpression AsSExpression() => new SExpression.List(new[] { DataType.AsSExpression(), new SExpression.UnquotedSymbol(Name) });

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

    public SExpression AsSExpression() => Name is null ? DataType.AsSExpression() : ToNamed().AsSExpression();

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

        throw new SExpression.FormatException($"Invalid optionally named data type: {sexpr}", sexpr);
    }
}