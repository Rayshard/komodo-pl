namespace Komodo.Compilation;

using Komodo.Utilities;

public enum TokenType
{
    Invalid,
    IntLit,
    Plus,
    Minus,
    Asterisk,
    ForwardSlash,
    EOF,
}

public record Token(TokenType Type, Location Location, string Value)
{
    public override string ToString() => $"{Type.ToString()}({Value}) at {Location}";
}