using System.Text;

namespace Komodo.Core.Utilities;

public record UTF8Char(UInt32 Value)
{
    public string Representation { get; } = Encoding.UTF32.GetString(BitConverter.GetBytes(Value));

    public static implicit operator UTF8Char(char c) => new UTF8Char(Convert.ToUInt32(c));
}