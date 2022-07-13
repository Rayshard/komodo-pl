namespace Komodo.Compilation.TypeSystem;

public interface TSType
{
    public bool IsSameAs(TSType other);
    public bool IsCastableTo(TSType other);
}