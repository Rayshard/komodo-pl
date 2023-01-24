use crate::compiler::cst::{BinaryOperatorKind, Expression, Module, Statement};

fn interpret_expression(expression: &Expression) -> Option<i64> {
    match expression {
        Expression::IntegerLiteral(_) => Some(1),
        Expression::Binary { left, op, right } => {
            let left = interpret_expression(left.as_ref())?;
            let right = interpret_expression(right.as_ref())?;

            Some(match op.kind() {
                BinaryOperatorKind::Add => left + right,
                BinaryOperatorKind::Subtract => left - right,
                BinaryOperatorKind::Multiply => left * right,
                BinaryOperatorKind::Divide => left / right,
            })
        }
        Expression::Parenthesized {
            open_parenthesis: _,
            expression,
            close_parenthesis: _,
        } => interpret_expression(expression.as_ref()),
    }
}

fn interpret_statement(statement: &Statement) -> Option<i64> {
    match statement {
        Statement::Expression(expression, _) => interpret_expression(expression),
    }
}

pub fn interpret_module(module: &Module) -> Option<i64> {
    let mut last = None;

    for statement in module.statements() {
        last = interpret_statement(statement);
    }

    last
}
