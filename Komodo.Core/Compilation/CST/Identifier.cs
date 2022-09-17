namespace Komodo.Core.Compilation.CST;

using Komodo.Core.Utilities;

public record Identifier(Token Token) : IExpression
{
    public NodeType NodeType => NodeType.Identifier;
    public TextLocation Location => Token.Location;
    public INode[] Children => new INode[] { Token };
}