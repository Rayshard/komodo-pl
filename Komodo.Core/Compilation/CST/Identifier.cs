namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public record Identifier(Token Token) : IExpression
{
    public NodeType NodeType => NodeType.Identifier;
    public TextLocation Location => Token.Location;
    public INode[] Children => new INode[] { Token };
}