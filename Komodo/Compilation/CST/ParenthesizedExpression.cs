namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public record ParenthesizedExpression(Token LParen, IExpression Expression, Token RParen) : IExpression
{
    public NodeType NodeType => NodeType.ParenthesizedExpression;
    public TextLocation Location => new TextLocation(Expression.Location.SourceName, LParen.Location.Start, RParen.Location.End);
    public INode[] Children => new INode[] { LParen, Expression, RParen };
}