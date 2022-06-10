using System.Text.Json;
using System.Text.Json.Nodes;
using Komodo.Compilation.ConcreteSyntaxTree;
using Komodo.Utilities;

namespace Komodo.Compilation;

public static class JsonSerializer
{
    private static T GetPropertyValue<T>(this JsonObject obj, string name, Func<JsonNode, T>? converter = null)
    {
        var value = obj[name] ?? throw new JsonException($"Object does not contain a property named {name}");

        if (converter == null) { return value.GetValue<T>() ?? throw new JsonException($"Object property \"{name}\" is not convertable to {typeof(T)}"); }
        else { return converter(value); }
    }

    private static void AssertPropertyValue<T>(this JsonObject obj, string name, T expectedValue, Func<JsonNode, T>? converter = null)
    {
        var value = obj.GetPropertyValue<T>(name, converter) ?? throw new NullReferenceException();

        if (!value.Equals(expectedValue))
            throw new JsonException($"Found: {value}\nExpected: {expectedValue}");
    }

    public static Token ParseToken(JsonNode json)
    {
        var obj = json.AsObject();

        var type = Enum.Parse<TokenType>(obj.GetPropertyValue<string>("type"));
        var location = Location.From(obj.GetPropertyValue<string>("location"));
        var value = obj.GetPropertyValue<string>("value");

        return new Token(type, location, value);
    }

    public static ICSTNode ParseCSTNode(JsonNode json)
    {
        var obj = json.AsObject();
        var nodeType = Enum.Parse<CSTNodeType>(obj.GetPropertyValue<string>("nodeType"));

        return nodeType switch
        {
            CSTNodeType.Literal => ParseCSTLiteral(json),
            CSTNodeType.BinopExpression => ParseCSTBinopExpression(json),
            CSTNodeType.ParenthesizedExpression => ParseParenthesizedExpression(json),
            CSTNodeType.BinaryOperator => ParseCSTBinaryOperator(json),
            _ => throw new NotImplementedException(nodeType.ToString())
        };
    }

    public static ICSTExpression ParseCSTExpression(JsonNode json)
    {
        var obj = json.AsObject();
        var nodeType = Enum.Parse<CSTNodeType>(obj.GetPropertyValue<string>("nodeType"));

        if (!nodeType.IsExpression())
            throw new ArgumentException($"{nodeType.ToString()} is not an expression");

        return (ICSTExpression)ParseCSTNode(json);
    }

    public static CSTLiteral ParseCSTLiteral(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CSTNodeType.Literal.ToString());

        var token = obj.GetPropertyValue("token", ParseToken);
        return new CSTLiteral(token);
    }

    public static CSTBinaryOperator ParseCSTBinaryOperator(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CSTNodeType.BinaryOperator.ToString());

        var token = obj.GetPropertyValue("token", ParseToken);
        return new CSTBinaryOperator(token);
    }

    public static CSTBinopExpression ParseCSTBinopExpression(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CSTNodeType.BinopExpression.ToString());

        var left = obj.GetPropertyValue("left", ParseCSTExpression);
        var right = obj.GetPropertyValue("right", ParseCSTExpression);
        var op = obj.GetPropertyValue("op", ParseCSTBinaryOperator);
        return new CSTBinopExpression(left, op, right);
    }

    public static CSTParenthesizedExpression ParseParenthesizedExpression(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CSTNodeType.ParenthesizedExpression.ToString());

        var lParen = obj.GetPropertyValue("lParen", ParseToken);
        var rParen = obj.GetPropertyValue("rParen", ParseToken);
        var expr = obj.GetPropertyValue("expr", ParseCSTExpression);
        return new CSTParenthesizedExpression(lParen, expr, rParen);
    }

    #region Serializers
    public static JsonNode Serialize(Token token)
    {
        return new JsonObject(new[] {
            KeyValuePair.Create<string, JsonNode?>("type", JsonValue.Create(token.Type.ToString())),
            KeyValuePair.Create<string, JsonNode?>("location", JsonValue.Create(token.Location.ToString())),
            KeyValuePair.Create<string, JsonNode?>("value", JsonValue.Create(token.Value.ToString())),
        });
    }

    public static JsonNode Serialize(ICSTNode node)
    {
        var properties = new Dictionary<string, JsonNode?>();
        properties.Add("nodeType", JsonValue.Create(node.NodeType.ToString()));
        //properties.Add("location", JsonValue.Create(node.Location.ToString()));

        switch (node)
        {
            case CSTLiteral(var token): properties.Add("token", Serialize(token)); break;
            case CSTBinaryOperator(var token): properties.Add("token", Serialize(token)); break;
            case CSTBinopExpression(var left, var op, var right):
                {
                    properties.Add("op", Serialize(op));
                    properties.Add("left", Serialize(left));
                    properties.Add("right", Serialize(right));
                }
                break;
            case CSTParenthesizedExpression(var lParen, var expr, var rParen):
                {
                    properties.Add("lParen", Serialize(lParen));
                    properties.Add("expr", Serialize(expr));
                    properties.Add("rParen", Serialize(rParen));
                }
                break;
            case CSTModule(var lBracket, var children, var rBracket):
                {
                    properties.Add("lBracket", Serialize(lBracket));
                    properties.Add("children", new JsonArray(children.Select(x => Serialize(x)).ToArray()));
                    properties.Add("rBracket", Serialize(rBracket));
                }
                break;
            default: throw new NotImplementedException(node.NodeType.ToString());
        }

        return new JsonObject(properties);
    }
    #endregion
}