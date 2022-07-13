namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public record Module(IStatement[] Statements, Token EOF);