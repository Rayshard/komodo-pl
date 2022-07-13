using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public enum BinaryOperation { Add, Sub, Multiply, Divide };

public record BinopExpression(IExpression Left, BinaryOperation Operation, IExpression Right, TextLocation Location) : IExpression
{
    public NodeType NodeType => NodeType.BinopExpression;
    public INode[] Children => new INode[] { Left, Right };

    public TSType TSType => (Left.TSType, Operation, Right.TSType) switch
    {
        (TSInt64, BinaryOperation.Add, TSInt64) => new TSInt64(),
        (TSInt64, BinaryOperation.Sub, TSInt64) => new TSInt64(),
        (TSInt64, BinaryOperation.Multiply, TSInt64) => new TSInt64(),
        (TSInt64, BinaryOperation.Divide, TSInt64) => new TSInt64(),
        var pattern => throw new NotImplementedException(pattern.ToString()),
    };
}
