using Komodo.Utilities;

namespace Komodo.Compilation.TypeSystem;

public interface TSType
{
    public bool IsSameAs(TSType other);
}