using Komodo.Utilities;
using Komodo.Compilation.TypeSystem;

namespace Komodo.Compilation.AST;

public record IntLiteral(long Value, TextLocation Location) : IExpression
{
    public NodeType NodeType => NodeType.IntLiteral;
    public TSType TSType => new TSInt64();
    public INode[] Children => new INode[] { };
}

public record BoolLiteral(bool Value, TextLocation Location) : IExpression
{
    public NodeType NodeType => NodeType.BoolLiteral;
    public TSType TSType => new TSBool();
    public INode[] Children => new INode[] { };
}
