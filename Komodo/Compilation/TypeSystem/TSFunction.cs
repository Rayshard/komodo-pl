using System.Collections.Immutable;
using Komodo.Utilities;

namespace Komodo.Compilation.TypeSystem;

public record TSFunction(TSType[] Parameters, TSType Return) : TSType
{
    public bool Accepts(IEnumerable<TSType> args)
    {
        if (args.Count() != Parameters.Count())
            return false;

        for (int i = 0; i < args.Count(); i++)
            if (!args.ElementAt(i).IsSameAs(Parameters[i]))
                return false;

        return true;
    }

    public bool IsSameAs(TSType other)
    {
        var function = other as TSFunction;
        if (function is null)
            return false;

        if (function.Parameters.Count() != Parameters.Count())
            return false;

        for (int i = 0; i < Parameters.Count(); i++)
            if (!function.Parameters[i].IsSameAs(Parameters[i]))
                return false;

        return function.Return.IsSameAs(Return);
    }

    public override string ToString() => $"{Utility.StringifyEnumerable("(", Parameters, ")", ", ")} -> {Return}";
}
