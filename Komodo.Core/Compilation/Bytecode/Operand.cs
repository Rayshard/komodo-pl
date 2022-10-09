using Komodo.Core.Utilities;

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

    public sealed override string ToString() => AsSExpression().ToString();

    public abstract record Constant : Operand, Source
    {
        new public abstract Bytecode.DataType DataType { get; }

        public record I8(SByte Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.I8();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("I8"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static I8 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.I8.Deserialize)
                     .ExpectItem(1, item => item.ExpectInt8(), out var value);

                return new I8(value);
            }
        }

        public record UI8(Byte Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.UI8();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("UI8"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static UI8 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.UI8.Deserialize)
                     .ExpectItem(1, item => item.ExpectUInt8(), out var value);

                return new UI8(value);
            }
        }

        public record I16(Int16 Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.I16();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("I16"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static I16 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.I16.Deserialize)
                     .ExpectItem(1, item => item.ExpectInt16(), out var value);

                return new I16(value);
            }
        }

        public record UI16(UInt16 Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.UI16();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("UI16"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static UI16 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.UI16.Deserialize)
                     .ExpectItem(1, item => item.ExpectUInt16(), out var value);

                return new UI16(value);
            }
        }

        public record I32(Int32 Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.I32();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("I32"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static I32 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.I32.Deserialize)
                     .ExpectItem(1, item => item.ExpectInt32(), out var value);

                return new I32(value);
            }
        }

        public record UI32(UInt32 Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.UI32();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("UI32"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static UI32 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.UI32.Deserialize)
                     .ExpectItem(1, item => item.ExpectUInt32(), out var value);

                return new UI32(value);
            }
        }

        public record I64(Int64 Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.I64();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("I64"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static I64 Deserialize(SExpression sexpr)
            {
                if (sexpr is SExpression.List list)
                {
                    sexpr.ExpectList()
                         .ExpectLength(2)
                         .ExpectItem(0, Bytecode.DataType.I64.Deserialize)
                         .ExpectItem(1, item => item.ExpectInt64(), out var value);

                    return new I64(value);
                }
                else { return new I64(sexpr.ExpectInt64()); }
            }
        }

        public record UI64(UInt64 Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.UI64();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("UI64"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static UI64 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.UI64.Deserialize)
                     .ExpectItem(1, item => item.ExpectUInt64(), out var value);

                return new UI64(value);
            }
        }

        public record F32(Single Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.F32();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("F32"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static F32 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.F32.Deserialize)
                     .ExpectItem(1, item => item.ExpectFloat(), out var value);

                return new F32(value);
            }
        }

        public record F64(Double Value) : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.F64();

            public override SExpression AsSExpression() => new SExpression.List(new[]{
                new SExpression.UnquotedSymbol("F64"),
                new SExpression.UnquotedSymbol(Value.ToString())
            });

            new public static F64 Deserialize(SExpression sexpr)
            {
                sexpr.ExpectList()
                     .ExpectLength(2)
                     .ExpectItem(0, Bytecode.DataType.F64.Deserialize)
                     .ExpectItem(1, item => item.ExpectDouble(), out var value);

                return new F64(value);
            }
        }

        public record True : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.Bool();

            public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("true");

            new public static True Deserialize(SExpression sexpr)
            {
                sexpr.ExpectUnquotedSymbol().ExpectValue("true");
                return new True();
            }
        }

        public record False : Constant
        {
            public override Bytecode.DataType DataType => new Bytecode.DataType.Bool();

            public override SExpression AsSExpression() => new SExpression.UnquotedSymbol("false");

            new public static False Deserialize(SExpression sexpr)
            {
                sexpr.ExpectUnquotedSymbol().ExpectValue("false");
                return new False();
            }
        }

        private static Func<SExpression, Constant>[] Deserializers => new Func<SExpression, Constant>[] {
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
            True.Deserialize,
            False.Deserialize,
        };

        public static Constant Deserialize(SExpression sexpr)
        {
            foreach (var deserializer in Deserializers)
            {
                try { return deserializer(sexpr); }
                catch { }
            }

            throw new SExpression.FormatException($"Invalid constant: {sexpr}", sexpr);
        }
    }

    public record DataType(Bytecode.DataType Value) : Operand
    {
        public override SExpression AsSExpression() => Value.AsSExpression();

        public static DataType Deserialize(SExpression sexpr) => new DataType(Bytecode.DataType.Deserialize(sexpr));
    }

    public record Identifier(string Value) : Operand
    {
        public override SExpression AsSExpression() => new SExpression.UnquotedSymbol(Value);

        public static Identifier Deserialize(SExpression sexpr) => new Identifier(sexpr.ExpectUnquotedSymbol().Value);
    }

    public abstract record Variable(SExpression Symbol, VSROCollection<SExpression> IDList) : Operand
    {
        public Variable(SExpression Symbol, params SExpression[] IDList) : this(Symbol, IDList.ToVSROCollection()) { }

        public override SExpression AsSExpression() => new SExpression.List(IDList.Prepend(Symbol));

        //TODO: Make idValidator be Func<IEnumerable<SExpression>, TId>
        public static TVariable Deserialize<TVariable, TId>(SExpression sexpr, SExpression symbol, Func<SExpression.List, TId> idValidator, Func<TId, TVariable> converter) where TVariable : Variable
        {
            sexpr.ExpectList().ExpectLength(1, null)
                 .ExpectItem(0, symbol)
                 .ExpectItems(items => idValidator(new SExpression.List(items)), out var id, 1);

            return converter(id);
        }
    }

    public abstract record Local(SExpression ID) : Variable(SYMBOL, ID), Source, Destination
    {
        private static readonly SExpression SYMBOL = new SExpression.UnquotedSymbol("local");

        public record Indexed(UInt64 Index) : Local(SExpression.UInt64(Index))
        {
            new public static Indexed Deserialize(SExpression sexpr)
                => Variable.Deserialize<Indexed, UInt64>(sexpr, SYMBOL, idList => idList.ExpectLength(1)[0].ExpectUInt64(), index => new Indexed(index));
        }

        public record Named(string Name) : Local(new SExpression.UnquotedSymbol(Name))
        {
            new public static Named Deserialize(SExpression sexpr)
                => Variable.Deserialize<Named, string>(sexpr, SYMBOL, idList => idList.ExpectLength(1)[0].ExpectUnquotedSymbol().Value, name => new Named(name));
        }

        public static Local Deserialize(SExpression sexpr)
        {
            try { return Indexed.Deserialize(sexpr); }
            catch { }

            try { return Named.Deserialize(sexpr); }
            catch { }

            throw new SExpression.FormatException($"Invalid local operand: {sexpr}", sexpr);
        }
    }

    public abstract record Arg(SExpression ID) : Variable(SYMBOL, ID), Source
    {
        private static readonly SExpression SYMBOL = new SExpression.UnquotedSymbol("arg");

        public record Indexed(UInt64 Index) : Arg(SExpression.UInt64(Index))
        {
            new public static Indexed Deserialize(SExpression sexpr)
                => Variable.Deserialize<Indexed, UInt64>(sexpr, SYMBOL, idList => idList.ExpectLength(1)[0].ExpectUInt64(), index => new Indexed(index));
        }

        public record Named(string Name) : Arg(new SExpression.UnquotedSymbol(Name))
        {
            new public static Named Deserialize(SExpression sexpr)
                => Variable.Deserialize<Named, string>(sexpr, SYMBOL, idList => idList.ExpectLength(1)[0].ExpectUnquotedSymbol().Value, name => new Named(name));
        }

        public static Arg Deserialize(SExpression sexpr)
        {
            try { return Indexed.Deserialize(sexpr); }
            catch { }

            try { return Named.Deserialize(sexpr); }
            catch { }

            throw new SExpression.FormatException($"Invalid arg operand: {sexpr}", sexpr);
        }
    }

    public record Global(string Module, string Name) : Variable(SYMBOL, new SExpression.UnquotedSymbol(Module), new SExpression.UnquotedSymbol(Name)), Source, Destination
    {
        private static readonly SExpression SYMBOL = new SExpression.UnquotedSymbol("global");

        public static Global Deserialize(SExpression sexpr)
            => Variable.Deserialize<Global, (string Module, string Name)>(
                sexpr,
                SYMBOL,
                idList =>
                {
                    idList.ExpectLength(2)
                          .ExpectItem(0, item => item.ExpectUnquotedSymbol().Value, out var module)
                          .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name);

                    return (module, name);
                },
                id => new Global(id.Module, id.Name)
            );
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

    public record Array(Bytecode.DataType ElementType, VSROCollection<Source> Elements) : Operand, Source
    {
        public override SExpression AsSExpression() => new SExpression.List(
            Elements.Select(element => element.AsSExpression())
                    .Prepend(ElementType.AsSExpression())
                    .Prepend(new SExpression.UnquotedSymbol("array"))
        );

        public static Array Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList().ExpectLength(2, null)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("array"))
                 .ExpectItem(1, Bytecode.DataType.Deserialize, out var elementType)
                 .ExpectItems(DeserializeSource, out var elements, 2);

            return new Array(elementType, elements.ToVSROCollection());
        }
    }

    public record Data(string Module, string Name) : Variable(SYMBOL, new SExpression.UnquotedSymbol(Module), new SExpression.UnquotedSymbol(Name)), Source
    {
        private static readonly SExpression SYMBOL = new SExpression.UnquotedSymbol("data");

        public static Data Deserialize(SExpression sexpr)
            => Variable.Deserialize<Data, (string Module, string Name)>(
                sexpr,
                SYMBOL,
                idList =>
                {
                    idList.ExpectLength(2)
                          .ExpectItem(0, item => item.ExpectUnquotedSymbol().Value, out var module)
                          .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name);

                    return (module, name);
                },
                id => new Data(id.Module, id.Name)
            );
    }

    public record Typeof(Bytecode.DataType Type) : Operand, Source
    {
        public override SExpression AsSExpression() => new SExpression.List(new[]{
            new SExpression.UnquotedSymbol("typeof"),
            Type.AsSExpression()
        });

        public static Typeof Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("typeof"))
                 .ExpectItem(1, Bytecode.DataType.Deserialize, out var type);

            return new Typeof(type);
        }
    }

    public record Null(Bytecode.DataType ValueType) : Operand, Source
    {
        public override SExpression AsSExpression() => new SExpression.List(new[]{
            new SExpression.UnquotedSymbol("null"),
            ValueType.AsSExpression()
        });

        public static Null Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(2)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("null"))
                 .ExpectItem(1, Bytecode.DataType.Deserialize, out var valueType);

            return new Null(valueType);
        }
    }

    public record Function(string ModuleName, string FunctionName) : Operand, Source
    {
        public override SExpression AsSExpression() => new SExpression.List(new[]{
            new SExpression.UnquotedSymbol("func"),
            new SExpression.UnquotedSymbol(ModuleName),
            new SExpression.UnquotedSymbol(FunctionName)
        });

        public static Function Deserialize(SExpression sexpr)
        {
            sexpr.ExpectList()
                 .ExpectLength(3)
                 .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("func"))
                 .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var moduleName)
                 .ExpectItem(2, item => item.ExpectUnquotedSymbol().Value, out var functionName);

            return new Function(moduleName, functionName);
        }
    }

    private static Func<SExpression, Source>[] SourceDeserializers => new Func<SExpression, Source>[] {
        Constant.Deserialize,
        Local.Deserialize,
        Arg.Deserialize,
        Global.Deserialize,
        Stack.Deserialize,
        Array.Deserialize,
        Data.Deserialize,
        Null.Deserialize,
        Typeof.Deserialize,
        Function.Deserialize,
    };

    private static Func<SExpression, Destination>[] DestinationDeserializers => new Func<SExpression, Destination>[] {
        Local.Deserialize,
        Global.Deserialize,
        Stack.Deserialize,
    };

    public static Source DeserializeSource(SExpression sexpr)
    {
        foreach (var deserializer in SourceDeserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid source operand: {sexpr}", sexpr);
    }

    public static Destination DeserializeDestination(SExpression sexpr)
    {
        foreach (var deserializer in DestinationDeserializers)
        {
            try { return deserializer(sexpr); }
            catch { }
        }

        throw new SExpression.FormatException($"Invalid destination operand: {sexpr}", sexpr);
    }
}