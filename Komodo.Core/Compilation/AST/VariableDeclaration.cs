using Komodo.Core.Utilities;
using Komodo.Core.Compilation.TypeSystem;

namespace Komodo.Core.Compilation.AST;

public record VariableDeclaration(Symbol.Variable Symbol, IExpression Expression, TextLocation Location) : IStatement
{
    public NodeType NodeType => NodeType.VariableDeclaration;
    public INode[] Children => new INode[] { Expression };
}