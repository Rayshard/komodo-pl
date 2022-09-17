using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.TypeSystem;

public interface TSType
{
    public bool IsSameAs(TSType other);
}