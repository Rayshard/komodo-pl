using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.TypeSystem;

public interface TSPrimitive : TSType { }

public record TSUnit() : TSPrimitive
{
    public bool IsSameAs(TSType other) => other is TSUnit;

    public override string ToString() => "Unit";
}

public record TSInt64() : TSPrimitive
{
    public bool IsSameAs(TSType other) => other is TSInt64;

    public override string ToString() => "I64";
}

public record TSBool() : TSPrimitive
{
    public bool IsSameAs(TSType other) => other is TSBool;

    public override string ToString() => "Bool";
}
