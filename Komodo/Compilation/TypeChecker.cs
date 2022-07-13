using Komodo.Compilation.CST;
using Komodo.Compilation.AST;
using Komodo.Utilities;

namespace Komodo.Compilation;

public static class TypeChecker
{
    public static AST.IExpression TypeCheck(CST.Literal node)
    {
        switch (node.LiteralType)
        {
            case LiteralType.Int: return new AST.IntLiteral(long.Parse(node.Token.Value), node.Location);
            case LiteralType.Bool: return new AST.BoolLiteral(bool.Parse(node.Token.Value), node.Location);
            default: throw new NotImplementedException(node.NodeType.ToString());
        }
    }

    public static AST.BinopExpression TypeCheck(CST.BinopExpression node)
    {
        var left = TypeCheck(node.Left);
        var right = TypeCheck(node.Right);

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

    public static AST.INode TypeCheck(CST.INode node) => node switch
    {
        CST.Literal => TypeCheck((CST.Literal)node),
        CST.BinopExpression => TypeCheck((CST.BinopExpression)node),
        CST.ParenthesizedExpression(_, var expr, _) => TypeCheck(expr),
        _ => throw new NotImplementedException(node.NodeType.ToString())
    };

    public static AST.IExpression TypeCheck(CST.IExpression node) => (AST.IExpression)TypeCheck((CST.INode)node);
    public static AST.IStatement TypeCheck(CST.IStatement node) => (AST.IStatement)TypeCheck((CST.IStatement)node);
}