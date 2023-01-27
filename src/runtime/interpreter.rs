use crate::compiler::cst::{BinaryOperatorKind, Expression, Script, Statement};

#[derive(Debug)]
pub enum Value {
    Unit,
    I64(i64),
    String(String),
    Object(String),
}

pub type InterpretError = String;

pub type InterpretResult = Result<Value, InterpretError>;

fn interpret_expression(expression: &Expression) -> InterpretResult {
    match expression {
        Expression::IntegerLiteral(token) => Ok(Value::I64(0)),
        Expression::Binary { left, op, right } => {
            let left = interpret_expression(left.as_ref())?;
            let right = interpret_expression(right.as_ref())?;

            match (left, op.kind(), right) {
                (Value::I64(left), BinaryOperatorKind::Add, Value::I64(right)) => {
                    Ok(Value::I64(left + right))
                }
                (Value::I64(left), BinaryOperatorKind::Subtract, Value::I64(right)) => {
                    Ok(Value::I64(left - right))
                }
                (Value::I64(left), BinaryOperatorKind::Multiply, Value::I64(right)) => {
                    Ok(Value::I64(left * right))
                }
                (Value::I64(left), BinaryOperatorKind::Divide, Value::I64(right)) => {
                    Ok(Value::I64(left / right))
                }
                (Value::Object(left), BinaryOperatorKind::MemberAccess, Value::Object(right)) => {
                    Ok(Value::Object(format!("{left}.{right}")))
                }
                (left, op, right) => Err(format!(
                    "Unable to perform operation '{op:?}' on {left:?} and {right:?}"
                )),
            }
        }
        Expression::Parenthesized {
            open_parenthesis: _,
            expression,
            close_parenthesis: _,
        } => interpret_expression(expression.as_ref()),
        Expression::Identifier(token) => Ok(Value::Object("id".to_string())),
        Expression::Call(head, _, arg, _) => {
            let head = interpret_expression(head.as_ref())?;
            let arg = interpret_expression(arg.as_ref())?;

            match (head, arg) {
                (Value::Object(head), Value::String(arg)) if head == "io.stdout.print_line" => {
                    println!("Hello, World!");
                    Ok(Value::Unit)
                }
                (head, arg) => Err(format!("Unable to perform call expression on {head:?} with {arg:?}")),
            }
        }
    }
}

fn interpret_statement(statement: &Statement) -> InterpretResult {
    match statement {
        Statement::Expression(expression, _) => interpret_expression(expression),
        Statement::Import {
            keyword: _,
            path: _,
            semicolon: _,
        } => Ok(Value::Unit),
    }
}

pub fn interpret_script(script: &Script) -> InterpretResult {
    let mut last = Value::Unit;

    for statement in script.statements() {
        last = interpret_statement(statement)?;
    }

    Ok(last)
}
