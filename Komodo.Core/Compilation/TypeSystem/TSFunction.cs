using System.Collections.ObjectModel;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.TypeSystem;

public record TSFunction(VSROCollection<TSType> Parameters, TSType Return) : TSType
{
    public bool Accepts(ReadOnlyCollection<TSType> args)
    {
        if (args.Count != Parameters.Count)
            return false;

        for (int i = 0; i < args.Count; i++)
            if (!args.ElementAt(i).IsSameAs(Parameters[i]))
                return false;

        return true;
    }

    public bool IsSameAs(TSType other)
    {
        var function = other as TSFunction;
        if (function is null)
            return false;

        if (function.Parameters.Count != Parameters.Count)
            return false;

        for (int i = 0; i < Parameters.Count; i++)
            if (!function.Parameters[i].IsSameAs(Parameters[i]))
                return false;

        return function.Return.IsSameAs(Return);
    }

    public override string ToString() => $"{Utility.Stringify(Parameters, ", ", ("(", ")"))} -> {Return}";
}
