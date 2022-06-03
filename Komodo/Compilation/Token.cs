using Komodo.Utilities;

namespace Komodo.Compilation
{
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

    public class Token
    {
        public TokenType Type { get; }
        public Location Location { get; }
        public string Value {get; }

        public Token(TokenType type, Location loc, string value)
        {
            Type = type;
            Location = loc;
            Value = value;
        }

        public override string ToString() => $"{Type.ToString()}({Value}) at {Location}";
    }
}