namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public record Module(TextSource Source, IStatement[] Statements, Token EOF);