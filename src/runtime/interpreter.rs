use std::collections::HashMap;

use crate::compiler::{
    ast::{
        expression::Expression, import_path::ImportPath, literal::Literal, statement::Statement,
        ExpressionNode, ImportPathNode, LiteralNode, ScriptNode, StatementNode,
    },
    cst::binary_operator::BinaryOperatorKind,
};

#[derive(Debug, Clone)]
pub enum Value {
    Unit,
    I64(i64),
    String(String),
    Object {
        name: String,
        members: HashMap<String, Value>,
    },
    Module {
        name: String,
        members: HashMap<String, Value>,
    },
    Function(String),
}

pub type InterpretError = String;
pub type InterpretResult<T> = Result<T, InterpretError>;

type Interpreter<'source, TIn, TOut> = fn(item: &TIn, ctx: &Context) -> InterpretResult<TOut>;
pub type Context = HashMap<String, Value>;

pub fn interpret_consecutive<TIn, TOut>(
    items: &[TIn],
    interpreter: Interpreter<TIn, TOut>,
    ctx: &Context,
) -> InterpretResult<Vec<TOut>> {
    let mut results = vec![];

    for item in items {
        results.push(interpreter(item, ctx)?);
    }

    Ok(results)
}

fn interpret_literal(literal: &LiteralNode) -> InterpretResult<Value> {
    match literal.instance() {
        Literal::Int64(value) => Ok(Value::I64(value.clone())),
        Literal::String(value) => Ok(Value::String(value.clone())),
    }
}

fn interpret_expression(expression: &ExpressionNode, ctx: &Context) -> InterpretResult<Value> {
    match expression.instance() {
        Expression::Literal(literal) => interpret_literal(literal),
        Expression::Binary { left, op, right } => {
            let left = interpret_expression(left.as_ref(), ctx)?;
            let right = interpret_expression(right.as_ref(), ctx)?;

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
                (Value::String(left), BinaryOperatorKind::Add, Value::String(right)) => {
                    Ok(Value::String(left + &right))
                }
                (left, op, right) => Err(format!(
                    "Unable to perform operation '{op:?}' on {left:?} and {right:?}"
                )),
            }
        }
        Expression::Call { head, args } => {
            let head = interpret_expression(head.as_ref(), ctx)?;
            let args = interpret_consecutive(args, interpret_expression, ctx)?;

            match (head, &args[..]) {
                (Value::Function(function_name), [Value::String(arg)])
                    if function_name == "std.io.stdout.print_line" =>
                {
                    println!("{arg}");
                    Ok(Value::Unit)
                }
                head => Err(format!("Unable to perform call operation on {head:?}")),
            }
        }
        Expression::MemberAccess { head, member } => {
            let head = interpret_expression(head.as_ref(), ctx)?;

            match head {
                Value::Object { name, members } => match members.get(member) {
                    Some(member) => Ok(member.clone()),
                    None => Err(format!("No member with name {member} exists in {name}")),
                },
                Value::Module { name, members } => match members.get(member) {
                    Some(member) => Ok(member.clone()),
                    None => Err(format!("No member with name {member} exists in {name}")),
                },
                head => Err(format!("Value has not accessable members: {head:?}")),
            }
        }
        Expression::Identifier(id) => Ok(ctx.get(id).unwrap().clone()),
    }
}

fn interpret_import_path<'value>(
    node: &ImportPathNode,
    ctx: &'value Context,
) -> InterpretResult<(String, &'value Value)> {
    match node.instance() {
        ImportPath::Simple(name) => Ok((name.to_string(), ctx.get(name).unwrap())),
        ImportPath::Complex { head, member } => {
            let (head_name, head_value) = interpret_import_path(head, ctx)?;

            match head_value {
                Value::Object { name, members } => match members.get(member) {
                    Some(member_value) => Ok((format!("{head_name}.{member}"), member_value)),
                    None => Err(format!("No member with name {member} exists in {name}")),
                },
                Value::Module { name, members } => match members.get(member) {
                    Some(member_value) => Ok((format!("{head_name}.{member}"), member_value)),
                    None => Err(format!("No member with name {member} exists in {name}")),
                },
                head => Err(format!("Value has not accessable members: {head:?}")),
            }
        }
    }
}

fn interpret_statement(node: &StatementNode, ctx: &mut Context) -> InterpretResult<Value> {
    match node.instance() {
        Statement::Expression(expression) => interpret_expression(expression, ctx),
        Statement::Import {
            import_path,
            from_path: None,
        } => {
            let (path, value) = interpret_import_path(import_path, ctx)?;
            ctx.insert(path, value.clone());

            Ok(Value::Unit)
        }
        Statement::Import {
            import_path,
            from_path: Some(from_path),
        } => {
            let (_, from_value) = interpret_import_path(from_path, ctx)?;

            let (name, value) = match from_value {
                Value::Object { name: _, members } => interpret_import_path(import_path, members),
                Value::Module { name: _, members } => interpret_import_path(import_path, members),
                head => Err(format!("Value has not accessable members: {head:?}")),
            }
            .map(|(name, value)| (name, value.clone()))?;

            ctx.insert(name, value.clone());
            Ok(value.clone())
        }
    }
}

pub fn interpret_script(script: &ScriptNode, ctx: &Context) -> InterpretResult<Value> {
    let mut last = Value::Unit;
    let mut ctx = ctx.clone();

    for statement in script.instance().statements() {
        last = interpret_statement(statement, &mut ctx)?;
    }

    Ok(last)
}
