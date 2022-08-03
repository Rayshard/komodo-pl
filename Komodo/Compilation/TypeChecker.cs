using Komodo.Compilation.TypeSystem;
using Komodo.Utilities;

namespace Komodo.Compilation;

public static class TypeChecker
{
    public static AST.IExpression? TypeCheck(CST.Literal node, TypeSystem.Environment environment, Diagnostics? diagnostics)
    {
        switch (node.LiteralType)
        {
            case CST.LiteralType.Int: return new AST.IntLiteral(long.Parse(node.Token.Value), node.Location);
            case CST.LiteralType.Bool: return new AST.BoolLiteral(bool.Parse(node.Token.Value), node.Location);
            default: throw new NotImplementedException(node.NodeType.ToString());
        }
    }

    public static AST.BinopExpression? TypeCheck(CST.BinopExpression node, TypeSystem.Environment environment, Diagnostics? diagnostics)
    {
        var left = TypeCheck(node.Left, environment, diagnostics);
        if (left is null)
            return null;

        var right = TypeCheck(node.Right, environment, diagnostics);
        if (right is null)
            return null;

        var operation = node.Op.Operation switch
        {
            CST.BinaryOperation.Add => AST.BinaryOperation.Add,
            CST.BinaryOperation.Sub => AST.BinaryOperation.Sub,
            CST.BinaryOperation.Multiply => AST.BinaryOperation.Multiply,
            CST.BinaryOperation.Divide => AST.BinaryOperation.Divide,
            var op => throw new NotImplementedException(op.ToString())
        };

        var operatorKind = operation switch
        {
            AST.BinaryOperation.Add => OperatorKind.BinaryAdd,
            AST.BinaryOperation.Sub => OperatorKind.BinarySubtract,
            AST.BinaryOperation.Multiply => OperatorKind.BinaryMultiply,
            AST.BinaryOperation.Divide => OperatorKind.BinaryDivide,
            var op => throw new NotImplementedException(op.ToString())
        };

        var operatorOverload = environment.GetOperatorOverload(operatorKind, new TSType[] { left.TSType, right.TSType }, node.Op.Location, diagnostics, true);
        if(operatorOverload is null)
            return null;

        return new AST.BinopExpression(left, operation, right, operatorOverload.Operator.Return, node.Location);
    }

    public static AST.Identifier? TypeCheck(CST.Identifier node, TypeSystem.Environment environment, Diagnostics? diagnostics)
    {
        var symbol = environment.GetSymbol(node.Token.Value, node.Location, diagnostics, true);
        if (symbol is null)
            return null;

        switch (symbol)
        {

            case Symbol.Variable variable: return new AST.Identifier.Expression(node.Token.Value, node.Location, variable);
            case Symbol.Function function: return new AST.Identifier.Expression(node.Token.Value, node.Location, function);
            case Symbol.Typename typename: return new AST.Identifier.Typename(node.Token.Value, node.Location, typename);
            default: throw new NotImplementedException(symbol.GetType().ToString());
        }
    }

    public static AST.VariableDeclaration? TypeCheck(CST.VariableDeclaration node, TypeSystem.Environment environment, Diagnostics? diagnostics)
    {
        var expr = TypeCheck(node.Expression, environment, diagnostics);
        if (expr is null)
            return null;

        var symbol = new Symbol.Variable(node.Identifier.Value, expr.TSType, node.Location);
        return environment.AddSymbol(symbol, diagnostics) ? new AST.VariableDeclaration(symbol, expr, node.Location) : null;
    }

    public static AST.Module? TypeCheck(CST.Module module, TypeSystem.Environment environment, Diagnostics? diagnostics)
    {
        var stmts = from stmt in module.Statements select TypeCheck(stmt, environment, diagnostics);
        return new AST.Module(stmts.ToArray());
    }

    public static AST.INode? TypeCheck(CST.INode node, TypeSystem.Environment environment, Diagnostics? diagnostics) => node switch
    {
        CST.Literal => TypeCheck((CST.Literal)node, environment, diagnostics),
        CST.BinopExpression => TypeCheck((CST.BinopExpression)node, environment, diagnostics),
        CST.Identifier => TypeCheck((CST.Identifier)node, environment, diagnostics),
        CST.VariableDeclaration => TypeCheck((CST.VariableDeclaration)node, environment, diagnostics),
        CST.ParenthesizedExpression(_, var expr, _) => TypeCheck(expr, environment, diagnostics),
        _ => throw new NotImplementedException(node.NodeType.ToString())
    };

    public static AST.IExpression? TypeCheck(CST.IExpression node, TypeSystem.Environment environment, Diagnostics? diagnostics) => (AST.IExpression?)TypeCheck((CST.INode)node, environment, diagnostics);
    public static AST.IStatement? TypeCheck(CST.IStatement node, TypeSystem.Environment environment, Diagnostics? diagnostics) => (AST.IStatement?)TypeCheck((CST.INode)node, environment, diagnostics);
}