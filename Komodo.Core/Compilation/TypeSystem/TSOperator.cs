using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.TypeSystem;

public enum OperatorKind
{
    BinaryAdd,
    BinarySubtract,
    BinaryMultiply,
    BinaryDivide,
}

public abstract record TSOperator(OperatorKind Kind, VSROCollection<TSType> Operands, TSType Result) : TSFunction(Operands, Result)
{
    public abstract record Binary(OperatorKind Kind, TSType LHS, TSType RHS, TSType Result)
        : TSOperator(Kind, new[] { LHS, RHS }.ToVSROCollection(), Result)
    {
        public record Add(TSType LHS, TSType RHS, TSType Result) : Binary(OperatorKind.BinaryAdd, LHS, RHS, Result);
        public record Subtract(TSType LHS, TSType RHS, TSType Result) : Binary(OperatorKind.BinarySubtract, LHS, RHS, Result);
        public record Multiply(TSType LHS, TSType RHS, TSType Result) : Binary(OperatorKind.BinaryMultiply, LHS, RHS, Result);
        public record Divide(TSType LHS, TSType RHS, TSType Result) : Binary(OperatorKind.BinaryDivide, LHS, RHS, Result);
    }

    public sealed override string ToString() => $"{Kind}{base.ToString()}";
}
