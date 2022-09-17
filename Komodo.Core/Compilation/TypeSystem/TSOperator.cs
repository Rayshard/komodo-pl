namespace Komodo.Core.Compilation.TypeSystem;

public enum OperatorKind
{
    BinaryAdd,
    BinarySubtract,
    BinaryMultiply,
    BinaryDivide,
}

public abstract record TSOperator(OperatorKind Kind, TSType[] Operands, TSType Result) : TSFunction(Operands, Result)
{
    public record BinaryAdd(TSType LHS, TSType RHS, TSType Result) : TSOperator(OperatorKind.BinaryAdd, new[] { LHS, RHS }, Result)
    {
        public override string ToString() => base.ToString();
    }

    public record BinarySubtract(TSType LHS, TSType RHS, TSType Result) : TSOperator(OperatorKind.BinarySubtract, new[] { LHS, RHS }, Result)
    {
        public override string ToString() => base.ToString();
    }

    public record BinaryMultipy(TSType LHS, TSType RHS, TSType Result) : TSOperator(OperatorKind.BinaryMultiply, new[] { LHS, RHS }, Result)
    {
        public override string ToString() => base.ToString();
    }

    public record BinaryDivide(TSType LHS, TSType RHS, TSType Result) : TSOperator(OperatorKind.BinaryDivide, new[] { LHS, RHS }, Result)
    {
        public override string ToString() => base.ToString();
    }

    public override string ToString() => $"{Kind}{base.ToString()}";
}
