namespace Komodo.Compilation.ConcreteSyntaxTree;

using Komodo.Utilities;

public enum CSTNodeType
{
    Module,
    Symbol,
    Literal,
    BinopExpression,
    ParenthesizedExpression,
}

public interface ICSTNode
{
    public CSTNodeType NodeType { get; }
    public Location Location { get; }
    public ICSTNode[] Children { get; }
}

public record CSTAtom(CSTNodeType NodeType, Token Token) : ICSTNode
{
    public Location Location => Token.Location;
    public ICSTNode[] Children => new ICSTNode[] { };
}

public record Module(ICSTNode[] Children, Location Location) : ICSTNode
{
    public CSTNodeType NodeType => CSTNodeType.Module;
}

public interface ICSTExpression : ICSTNode { }

public record CSTLiteral(Token Token) : CSTAtom(CSTNodeType.Literal, Token), ICSTExpression { }
public record CSTBinop(Token Token) : CSTAtom(CSTNodeType.Symbol, Token) { }

public record CSTBinopExpression(ICSTExpression Left, CSTBinop Op, ICSTExpression Right) : ICSTExpression
{
    public CSTNodeType NodeType => CSTNodeType.BinopExpression;
    public Location Location => new Location(Op.Location.SourceFile, new Span(Left.Location.Span.Start, Right.Location.Span.End));
    public ICSTNode[] Children => new ICSTNode[] { Left, Op, Right };
}

public record ParenthesizedExpression(Token LParen, ICSTExpression Expression, Token RParen) : ICSTExpression
{
    public CSTNodeType NodeType => CSTNodeType.ParenthesizedExpression;
    public Location Location => new Location(Expression.Location.SourceFile, new Span(LParen.Location.Span.Start, RParen.Location.Span.End));
    public ICSTNode[] Children => new ICSTNode[] { new CSTAtom(CSTNodeType.Symbol, LParen), Expression, new CSTAtom(CSTNodeType.Symbol, RParen) };
}
