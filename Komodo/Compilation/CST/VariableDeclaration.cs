namespace Komodo.Compilation.CST;

using Komodo.Utilities;

public record VariableDeclaration(Token VarKeyword, Token Identifier, Token SingleEquals, IExpression Expression, Token Semicolon) : IStatement
{
    public NodeType NodeType => NodeType.VariableDeclaration;
    public TextLocation Location => new TextLocation(VarKeyword.Location.SourceName, VarKeyword.Location.Start, Semicolon.Location.End);
    public INode[] Children => new INode[] { VarKeyword, Identifier, SingleEquals, Expression, Semicolon };
}