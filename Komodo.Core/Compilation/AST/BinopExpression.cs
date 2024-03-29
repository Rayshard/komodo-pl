using Komodo.Core.Utilities;
using Komodo.Core.Compilation.TypeSystem;

namespace Komodo.Core.Compilation.AST;

public enum BinaryOperation { Add, Sub, Multiply, Divide };

public record BinopExpression(IExpression Left, BinaryOperation Operation, IExpression Right, TSType TSType, TextLocation Location) : IExpression
{
    public NodeType NodeType => NodeType.BinopExpression;
    public INode[] Children => new INode[] { Left, Right };
}
