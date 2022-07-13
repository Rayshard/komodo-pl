namespace Komodo.Compilation.TypeSystem;

public interface TSPrimitive : TSType { }

public record TSUnit : TSPrimitive
{
    public bool IsSameAs(TSType other) => other is TSUnit;
    public bool IsCastableTo(TSType other) => IsSameAs(other);
}

public record TSInt64 : TSPrimitive
{
    public bool IsSameAs(TSType other) => other is TSInt64;
    public bool IsCastableTo(TSType other) => IsSameAs(other) || other is TSBool;
}

public record TSBool : TSPrimitive
{
    public bool IsSameAs(TSType other) => other is TSBool;
    public bool IsCastableTo(TSType other) => IsSameAs(other) || other is TSInt64;
}
