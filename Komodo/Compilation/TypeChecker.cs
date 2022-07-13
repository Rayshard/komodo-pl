using Komodo.Utilities;

namespace Komodo.Compilation;

public static class TypeChecker
{
    public static AST.IExpression? TypeCheck(CST.Literal node, TypeSystem.Environment environment, Diagnostics diagnostics)
    {
        switch (node.LiteralType)
        {
            case CST.LiteralType.Int: return new AST.IntLiteral(long.Parse(node.Token.Value), node.Location);
            case CST.LiteralType.Bool: return new AST.BoolLiteral(bool.Parse(node.Token.Value), node.Location);
            default: throw new NotImplementedException(node.NodeType.ToString());
        }
    }

    public static AST.BinopExpression? TypeCheck(CST.BinopExpression node, TypeSystem.Environment environment, Diagnostics diagnostics)
    {
        var left = TypeCheck(node.Left, environment, diagnostics);
        var right = TypeCheck(node.Right, environment, diagnostics);

        var operation = node.Op.Operation switch
        {
            CST.BinaryOperation.Add => AST.BinaryOperation.Add,
            CST.BinaryOperation.Sub => AST.BinaryOperation.Sub,
            CST.BinaryOperation.Multiply => AST.BinaryOperation.Multiply,
            CST.BinaryOperation.Divide => AST.BinaryOperation.Divide,
            var op => throw new NotImplementedException(op.ToString())
        };

        return new AST.BinopExpression(left, operation, right, node.Location);
    }

    public static AST.SymbolExpression? TypeCheck(CST.IdentifierExpression node, TypeSystem.Environment environment, Diagnostics diagnostics)
    {
        var symbol = environment.GetSymbol(node.ID.Value, node.Location, diagnostics, true);
        return new AST.IdentifierExpression(node.ID.Value, node.Location);
    }

    public static AST.INode? TypeCheck(CST.INode node, TypeSystem.Environment environment, Diagnostics diagnostics) => node switch
    {
        CST.Literal => TypeCheck((CST.Literal)node, environment, diagnostics),
        CST.BinopExpression => TypeCheck((CST.BinopExpression)node, environment, diagnostics),
        CST.ParenthesizedExpression(_, var expr, _) => TypeCheck(expr, environment, diagnostics),
        _ => throw new NotImplementedException(node.NodeType.ToString())
    };

    public static AST.IExpression? TypeCheck(CST.IExpression node, TypeSystem.Environment environment, Diagnostics diagnostics) => (AST.IExpression)TypeCheck((CST.INode)node, environment, diagnostics);
    public static AST.IStatement? TypeCheck(CST.IStatement node, TypeSystem.Environment environment, Diagnostics diagnostics) => (AST.IStatement)TypeCheck((CST.IStatement)node, environment, diagnostics);
}