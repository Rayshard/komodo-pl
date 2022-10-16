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
    public abstract record SignedInteger : Primitive;
    public abstract record UnsignedInteger : Primitive;

    public record I8 : SignedInteger
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

    public record UI8 : UnsignedInteger
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

    public record I16 : SignedInteger
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

    public record UI16 : UnsignedInteger
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

    public record I32 : SignedInteger
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

    public record UI32 : UnsignedInteger
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

    public record I64 : SignedInteger
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

    public record UI64 : UnsignedInteger
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

    public record Pointer(bool IsReadonly) : DataType
    {
        public sealed override UInt64 ByteSize => ByteSizeOf<Pointer>();
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(IsReadonly ? "ROPtr" : "RWPtr");
        public override string AsMangledString() => IsReadonly ? "roptr" : "rwptr";

        public bool IsReadWrite => !IsReadonly;

        new public static Pointer Deserialize(SExpression sexpr)
        {
            var isReadonly = sexpr.ExpectUnquotedSymbol().ExpectValue("ROPtr", "RWPtr").Value == "ROPtr";
            return new Pointer(isReadonly);
        }
    }

    public record Array(DataType ElementType) : Pointer(true)
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

    public record Reference(DataType ValueType) : Pointer(true)
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

    public record Function(VSROCollection<DataType> Parameters, VSROCollection<DataType> Returns) : Pointer(true)
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
        I8.Deserialize, UI8.Deserialize,
        I16.Deserialize, UI16.Deserialize,
        I32.Deserialize, UI32.Deserialize,
        I64.Deserialize, UI64.Deserialize,
        F32.Deserialize, F64.Deserialize,
        Bool.Deserialize,
        Pointer.Deserialize, Reference.Deserialize, Array.Deserialize, Function.Deserialize,
    };

    private static Func<SExpression, Primitive>[] PrimitiveDeserializers => new Func<SExpression, Primitive>[] {
        I8.Deserialize, UI8.Deserialize,
        I16.Deserialize, UI16.Deserialize,
        I32.Deserialize, UI32.Deserialize,
        I64.Deserialize, UI64.Deserialize,
        F32.Deserialize, F64.Deserialize,
        Bool.Deserialize,
    };

    private static Func<SExpression, SignedInteger>[] SignedIntegerDeserializers => new Func<SExpression, SignedInteger>[] {
        I8.Deserialize, I16.Deserialize, I32.Deserialize, I64.Deserialize,
    };

    private static Func<SExpression, UnsignedInteger>[] UnsignedIntegerDeserializers => new Func<SExpression, UnsignedInteger>[] {
        UI8.Deserialize, UI16.Deserialize, UI32.Deserialize, UI64.Deserialize,
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

    public static Primitive DeserializePrimitive(SExpression sexpr)
    {
        foreach (var deserializer in PrimitiveDeserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid primitive data type: {sexpr}", sexpr);
    }

    public static SignedInteger DeserializeSignedInteger(SExpression sexpr)
    {
        foreach (var deserializer in SignedIntegerDeserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid signed integer data type: {sexpr}", sexpr);
    }

    public static UnsignedInteger DeserializeUnsignedInteger(SExpression sexpr)
    {
        foreach (var deserializer in UnsignedIntegerDeserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid unsigned integer data type: {sexpr}", sexpr);
    }
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