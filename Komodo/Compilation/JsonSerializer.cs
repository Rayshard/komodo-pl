using System.Text.Json;
using System.Text.Json.Nodes;
using Komodo.Compilation.CST;
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

    public static CST.Token ParseToken(JsonNode json)
    {
        var obj = json.AsObject();

        var type = Enum.Parse<CST.TokenType>(obj.GetPropertyValue<string>("type"));
        var location = TextLocation.From(obj.GetPropertyValue<string>("location"));
        var value = obj.GetPropertyValue<string>("value");

        return new CST.Token(type, location, value);
    }

    public static CST.INode ParseCSTNode(JsonNode json)
    {
        var obj = json.AsObject();
        var nodeType = Enum.Parse<CST.NodeType>(obj.GetPropertyValue<string>("nodeType"));

        return nodeType switch
        {
            CST.NodeType.Literal => ParseCSTLiteral(json),
            CST.NodeType.BinopExpression => ParseCSTBinopExpression(json),
            CST.NodeType.ParenthesizedExpression => ParseCSTParenthesizedExpression(json),
            CST.NodeType.BinaryOperator => ParseCSTBinaryOperator(json),
            CST.NodeType.VariableDeclaration => ParseCSTVariableDeclaration(json),
            CST.NodeType.IdentifierExpression => ParseCSTIdentifierExpression(json),
            _ => throw new NotImplementedException(nodeType.ToString())
        };
    }

    public static CST.IExpression ParseCSTExpression(JsonNode json)
    {
        var obj = json.AsObject();
        var nodeType = Enum.Parse<CST.NodeType>(obj.GetPropertyValue<string>("nodeType"));

        if (!nodeType.IsExpression())
            throw new ArgumentException($"{nodeType.ToString()} is not an expression");

        return (CST.IExpression)ParseCSTNode(json);
    }

    public static CST.IStatement ParseCSTStatement(JsonNode json)
    {
        var obj = json.AsObject();
        var nodeType = Enum.Parse<CST.NodeType>(obj.GetPropertyValue<string>("nodeType"));

        if (!nodeType.IsStatement())
            throw new ArgumentException($"{nodeType.ToString()} is not a statement");

        return (CST.IStatement)ParseCSTNode(json);
    }

    public static CST.Literal ParseCSTLiteral(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CST.NodeType.Literal.ToString());

        var token = obj.GetPropertyValue("token", ParseToken);
        return new CST.Literal(token);
    }

    public static CST.BinaryOperator ParseCSTBinaryOperator(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CST.NodeType.BinaryOperator.ToString());

        var token = obj.GetPropertyValue("token", ParseToken);
        return new CST.BinaryOperator(token);
    }

    public static CST.BinopExpression ParseCSTBinopExpression(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CST.NodeType.BinopExpression.ToString());

        var left = obj.GetPropertyValue("left", ParseCSTExpression);
        var right = obj.GetPropertyValue("right", ParseCSTExpression);
        var op = obj.GetPropertyValue("op", ParseCSTBinaryOperator);
        return new CST.BinopExpression(left, op, right);
    }

    public static CST.ParenthesizedExpression ParseCSTParenthesizedExpression(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CST.NodeType.ParenthesizedExpression.ToString());

        var lParen = obj.GetPropertyValue("lParen", ParseToken);
        var rParen = obj.GetPropertyValue("rParen", ParseToken);
        var expr = obj.GetPropertyValue("expr", ParseCSTExpression);
        return new CST.ParenthesizedExpression(lParen, expr, rParen);
    }

    public static CST.IdentifierExpression ParseCSTIdentifierExpression(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CST.NodeType.IdentifierExpression.ToString());

        var id = obj.GetPropertyValue("id", ParseToken);
        return new CST.IdentifierExpression(id);
    }

    public static CST.VariableDeclaration ParseCSTVariableDeclaration(JsonNode json)
    {
        var obj = json.AsObject();
        obj.AssertPropertyValue("nodeType", CST.NodeType.VariableDeclaration.ToString());

        var varKeyword = obj.GetPropertyValue("varKeyword", ParseToken);
        var id = obj.GetPropertyValue("id", ParseToken);
        var singleEqualsSymbol = obj.GetPropertyValue("singleEquals", ParseToken);
        var expr = obj.GetPropertyValue("expr", ParseCSTExpression);
        var semicolon = obj.GetPropertyValue("semicolon", ParseToken);
        return new CST.VariableDeclaration(varKeyword, id, singleEqualsSymbol, expr, semicolon);
    }

    #region Serializers
    public static JsonNode Serialize(CST.Token token)
    {
        return new JsonObject(new[] {
            KeyValuePair.Create<string, JsonNode?>("type", JsonValue.Create(token.Type.ToString())),
            KeyValuePair.Create<string, JsonNode?>("location", JsonValue.Create(token.Location.ToString())),
            KeyValuePair.Create<string, JsonNode?>("value", JsonValue.Create(token.Value.ToString())),
        });
    }

    public static JsonNode Serialize(CST.Module module)
    {
        return new JsonObject(new[] {
            KeyValuePair.Create<string, JsonNode?>("sourceName", JsonValue.Create(module.Source.Name)),
            KeyValuePair.Create<string, JsonNode?>("statements", new JsonArray(module.Statements.Select(stmt => Serialize(stmt)).ToArray())),
            KeyValuePair.Create<string, JsonNode?>("eofToken", Serialize(module.EOF)),
        });
    }

    public static JsonNode Serialize(CST.INode node)
    {
        var properties = new Dictionary<string, JsonNode?>();
        properties.Add("nodeType", JsonValue.Create(node.NodeType.ToString()));
        //properties.Add("location", JsonValue.Create(node.Location.ToString()));

        switch (node)
        {
            case CST.Literal(var token): properties.Add("token", Serialize(token)); break;
            case CST.BinaryOperator(var token): properties.Add("token", Serialize(token)); break;
            case CST.BinopExpression(var left, var op, var right):
                {
                    properties.Add("op", Serialize(op));
                    properties.Add("left", Serialize(left));
                    properties.Add("right", Serialize(right));
                }
                break;
            case CST.ParenthesizedExpression(var lParen, var expr, var rParen):
                {
                    properties.Add("lParen", Serialize(lParen));
                    properties.Add("expr", Serialize(expr));
                    properties.Add("rParen", Serialize(rParen));
                }
                break;
            case CST.VariableDeclaration(var varKeyword, var id, var singleEqualsSymbol, var expr, var semicolon):
                {
                    properties.Add("varKeyword", Serialize(varKeyword));
                    properties.Add("id", Serialize(id));
                    properties.Add("singleEquals", Serialize(singleEqualsSymbol));
                    properties.Add("expr", Serialize(expr));
                    properties.Add("semicolon", Serialize(semicolon));
                }
                break;
            case CST.IdentifierExpression(var id): properties.Add("id", Serialize(id)); break;
            default: throw new NotImplementedException(node.NodeType.ToString());
        }

        return new JsonObject(properties);
    }
    #endregion
}