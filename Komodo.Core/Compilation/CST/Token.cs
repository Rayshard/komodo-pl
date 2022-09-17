using Komodo.Utilities;

namespace Komodo.Compilation.CST;

public enum TokenType
{
    Invalid,
    IntLit,
    Identifier,
    Plus,
    Minus,
    Asterisk,
    ForwardSlash,
    LParen,
    RParen,
    LCBracket,
    RCBracket,
    SingleEquals,
    Semicolon,
    KW_VAR,
    EOF,
}

public record Token(TokenType Type, TextLocation Location, string Value) : INode
{
    public NodeType NodeType => NodeType.Token;
    public INode[] Children => new INode[] { };
    public override string ToString() => $"{Type.ToString()}({Value}) at {Location}";
}
