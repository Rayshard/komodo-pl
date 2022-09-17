namespace Komodo.Core.Compilation.CST;

using Komodo.Core.Utilities;

public enum BinaryOperation { Add, Sub, Multiply, Divide };
public enum BinaryOperationAssociativity { Left, Right, None };

public record BinaryOperator(Token Token) : INode
{
    public NodeType NodeType => NodeType.BinaryOperator;
    public TextLocation Location => Token.Location;
    public INode[] Children => new INode[] { Token };

    public BinaryOperation Operation => Token.Type switch
    {
        TokenType.Plus => BinaryOperation.Add,
        TokenType.Minus => BinaryOperation.Sub,
        TokenType.Asterisk => BinaryOperation.Multiply,
        TokenType.ForwardSlash => BinaryOperation.Divide,
        var type => throw new NotImplementedException(type.ToString()),
    };

    public BinaryOperationAssociativity Asssociativity => Operation switch
    {
        BinaryOperation.Add => BinaryOperationAssociativity.Left,
        BinaryOperation.Sub => BinaryOperationAssociativity.Left,
        BinaryOperation.Multiply => BinaryOperationAssociativity.Left,
        BinaryOperation.Divide => BinaryOperationAssociativity.Left,
        var type => throw new NotImplementedException(type.ToString()),
    };

    public int Precedence => Operation switch
    {
        BinaryOperation.Add => 0,
        BinaryOperation.Sub => 0,
        BinaryOperation.Multiply => 1,
        BinaryOperation.Divide => 1,
        var op => throw new NotImplementedException(op.ToString()),
    };
}