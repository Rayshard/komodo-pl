use crate::compiler::{ast::{
    expression::Expression, literal::Literal, script::Script, statement::Statement, Node,
}, parsing::cst::binary_operator::BinaryOperatorKind};

#[derive(Debug)]
pub enum Value {
    Unit,
    I64(i64),
    String(String),
    Object(String),
}

pub type InterpretError = String;

pub type InterpretResult = Result<Value, InterpretError>;

fn interpret_literal(literal: &Node<Literal>) -> InterpretResult {
    match literal.instance() {
        Literal::Int64(value) => Ok(Value::I64(value.clone())),
    }
}

fn interpret_expression(expression: &Node<Expression>) -> InterpretResult {
    match expression.instance() {
        Expression::Literal(literal) => interpret_literal(literal),
        //Expression::StringLiteral(token) => Ok(Value::String(token.value().to_string())),
        Expression::Binary { left, op, right } => {
            let left = interpret_expression(left.as_ref())?;
            let right = interpret_expression(right.as_ref())?;

            match (left, op, right) {
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
        //Expression::Identifier(token) => Ok(Value::Object(token.value().to_string())),
        // Expression::MemberAccess {
        //     head,
        //     dot: _,
        //     member,
        // } => {
        //     let head = interpret_expression(head.as_ref())?;

        //     if let Value::Object(head) = head {
        //         Ok(Value::Object(format!("{head}.{}", member.value())))
        //     } else {
        //         Err(format!("Unable to access non-object expression: {head:?}"))
        //     }
        // }
        // Expression::Call {
        //     head,
        //     open_parenthesis: _,
        //     arg,
        //     close_parenthesis: _,
        // } => {
        //     let head = interpret_expression(head.as_ref())?;
        //     let arg = interpret_expression(arg.as_ref())?;

        //     match head {
        //         Value::Object(head) if head == "stdout.print_line" => {
        //             match arg {
        //                 Value::Unit => println!("()"),
        //                 Value::I64(value) => println!("{value}"),
        //                 Value::String(value) => println!("{value}"),
        //                 Value::Object(value) => println!("{value}"),
        //             }

        //             Ok(Value::Unit)
        //         }
        //         head => Err(format!("Unable to perform call operation on {head:?}")),
        //     }
        // }
        // Expression::Unary { operand, op } => {
        //     let operand = interpret_expression(operand.as_ref())?;

        //     Err(format!(
        //         "Unable to perform unary operation {op:?} on {operand:?}"
        //     ))
        // }
    }
}

fn interpret_statement(statement: &Node<Statement>) -> InterpretResult {
    match statement.instance() {
        Statement::Expression(expression) => interpret_expression(expression),
    }
}

pub fn interpret_script(script: &Node<Script>) -> InterpretResult {
    let mut last = Value::Unit;

    for statement in script.instance().statements() {
        last = interpret_statement(statement)?;
    }

    Ok(last)
}
