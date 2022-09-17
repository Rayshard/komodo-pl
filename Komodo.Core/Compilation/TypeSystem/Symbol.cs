using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.TypeSystem;

public interface Symbol
{
    public string Name { get; }
    public TextLocation DefinitionLocation { get; }

    public record Typename(string Name, TSType TSType, TextLocation DefinitionLocation) : Symbol;
    public record Variable(string Name, TSType TSType, TextLocation DefinitionLocation) : Symbol;
    public record Function(string Name, TSFunction TSType, TextLocation DefinitionLocation) : Symbol;
}