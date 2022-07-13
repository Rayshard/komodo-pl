namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public record IdentifierExpression(Token ID) : IExpression
{
    public NodeType NodeType => NodeType.IdentifierExpression;
    public TextLocation Location => new TextLocation(ID.Location.SourceName, ID.Location.Start, ID.Location.End);
    public INode[] Children => new INode[] { ID };
}