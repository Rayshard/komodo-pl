namespace Komodo.Core.Compilation.CST;

using Komodo.Core.Utilities;

public record BinopExpression(IExpression Left, BinaryOperator Op, IExpression Right) : IExpression
{
    public NodeType NodeType => NodeType.BinopExpression;
    public TextLocation Location => new TextLocation(Op.Location.SourceName, Left.Location.Start, Right.Location.End);
    public INode[] Children => new INode[] { Left, Op, Right };
}