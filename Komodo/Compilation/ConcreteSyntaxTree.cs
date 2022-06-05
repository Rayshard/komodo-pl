namespace Komodo.Compilation.ConcreteSyntaxTree;

using System.Text.Json;
using System.Text.Json.Serialization;
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

public enum LiteralType { Int, String, Bool, Char }

public record CSTLiteral(Token Token) : CSTAtom(CSTNodeType.Literal, Token), ICSTExpression
{
    public LiteralType LiteralType => Token.Type switch
    {
        TokenType.IntLit => LiteralType.Int,
        var type => throw new NotImplementedException(type.ToString()),
    };
}

public enum BinaryOperation { Add, Sub, Multiply, Divide };
public enum BinaryOperationAssociativity { Left, Right, None };

public record CSTBinop(Token Token) : CSTAtom(CSTNodeType.Symbol, Token)
{
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
        BinaryOperation.Add => 1,
        BinaryOperation.Sub => 1,
        BinaryOperation.Multiply => 2,
        BinaryOperation.Divide => 2,
        var op => throw new NotImplementedException(op.ToString()),
    };
}

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

public class CSTNodeJsonConverter : JsonConverter<ICSTNode>
{
    public override ICSTNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ICSTNode node, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", node.NodeType.ToString());
        writer.WriteString("location", node.Location.ToString());

        switch (node)
        {
            case CSTBinopExpression(var left, var op, var right):
                {
                    writer.WritePropertyName("left");
                    writer.WriteRawValue(JsonSerializer.Serialize((ICSTNode?)left, options));
                    writer.WriteString("op", op.Operation.ToString());
                    writer.WritePropertyName("right");
                    writer.WriteRawValue(JsonSerializer.Serialize((ICSTNode?)right, options));
                }
                break;
            case CSTLiteral(var token):
                {
                    var literal = (CSTLiteral)node;
                    writer.WriteString("literal_type", literal.LiteralType.ToString());
                    writer.WriteString("value", token.Value);
                }
                break;
            default: throw new NotImplementedException(node.NodeType.ToString());
        }

        writer.WriteEndObject();
    }
}
