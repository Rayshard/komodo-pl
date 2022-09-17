using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation;

public record CompilationContext(Dictionary<string, TextSource> SourceMap, string Entry, bool PrintTokens, bool PrintCST);