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

    public abstract record Variable(SExpression Symbol, params SExpression[] IDList) : Operand
    {
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

    public record Array(Bytecode.DataType ElementType, IList<Source> Elements) : Operand, Source
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

            return new Array(elementType, elements);
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

    public static Source DeserializeSource(SExpression sexpr)
    {
        try { return Constant.Deserialize(sexpr); }
        catch { }

        try { return Local.Deserialize(sexpr); }
        catch { }

        try { return Arg.Deserialize(sexpr); }
        catch { }

        try { return Global.Deserialize(sexpr); }
        catch { }

        try { return Stack.Deserialize(sexpr); }
        catch { }

        try { return Array.Deserialize(sexpr); }
        catch { }

        try { return Data.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid source operand: {sexpr}", sexpr);
    }

    public static Destination DeserializeDestination(SExpression sexpr)
    {
        try { return Local.Deserialize(sexpr); }
        catch { }

        try { return Global.Deserialize(sexpr); }
        catch { }

        try { return Stack.Deserialize(sexpr); }
        catch { }

        throw new SExpression.FormatException($"Invalid source operand: {sexpr}", sexpr);
    }
}