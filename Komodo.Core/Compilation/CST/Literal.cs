namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public enum LiteralType { Int, String, Bool, Char }

public record Literal(Token Token) : IExpression
{
    public NodeType NodeType => NodeType.Literal;
    public TextLocation Location => Token.Location;
    public INode[] Children => new INode[] { Token };

    public LiteralType LiteralType => Token.Type switch
    {
        TokenType.IntLit => LiteralType.Int,
        var type => throw new NotImplementedException(type.ToString()),
    };
}