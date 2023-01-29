use crate::compiler::parsing::cst::{expression::Expression, binary_operator::BinaryOperatorKind, script::Script, statement::Statement};


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
        Expression::IntegerLiteral(token) => Ok(Value::I64(token.value().parse().unwrap())),
        Expression::StringLiteral(token) => Ok(Value::String(token.value().to_string())),
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
        Expression::Identifier(token) => Ok(Value::Object(token.value().to_string())),
        Expression::MemberAccess {
            head,
            dot: _,
            member,
        } => {
            let head = interpret_expression(head.as_ref())?;

            if let Value::Object(head) = head {
                Ok(Value::Object(format!("{head}.{}", member.value())))
            } else {
                Err(format!("Unable to access non-object expression: {head:?}"))
            }
        }
        Expression::Call {
            head,
            open_parenthesis: _,
            arg,
            close_parenthesis: _,
        } => {
            let head = interpret_expression(head.as_ref())?;
            let arg = interpret_expression(arg.as_ref())?;

            match head {
                Value::Object(head) if head == "stdout.print_line" => {
                    match arg {
                        Value::Unit => println!("()"),
                        Value::I64(value) => println!("{value}"),
                        Value::String(value) => println!("{value}"),
                        Value::Object(value) => println!("{value}"),
                    }

                    Ok(Value::Unit)
                }
                head => Err(format!("Unable to perform call operation on {head:?}")),
            }
        }
        Expression::Unary { operand, op } => {
            let operand = interpret_expression(operand.as_ref())?;

            Err(format!(
                "Unable to perform unary operation {op:?} on {operand:?}"
            ))
        }
    }
}

fn interpret_statement(statement: &Statement) -> InterpretResult {
    match statement {
        Statement::Expression {
            expression,
            semicolon: _,
        } => interpret_expression(expression),
        Statement::Import {
            keyword_import: _,
            import_path: _,
            from_path: _,
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
