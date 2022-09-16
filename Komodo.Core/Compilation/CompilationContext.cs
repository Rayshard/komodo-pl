using Komodo.Utilities;

namespace Komodo.Compilation;

public record CompilationContext(Dictionary<string, TextSource> SourceMap, string Entry, bool PrintTokens, bool PrintCST);