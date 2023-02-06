use std::collections::HashMap;

use crate::compiler::{
    ast::{
        expression::{
            binary::Binary,
            call::Call,
            identifier::Identifier,
            literal::{Literal, LiteralKind},
            member_access::MemberAccess,
            Expression,
        },
        script::Script,
        statement::{import::Import, import_path::ImportPath, Statement},
    },
    cst::expression::binary_operator::BinaryOperatorKind,
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

fn interpret_literal(node: &Literal) -> InterpretResult<Value> {
    match node.kind() {
        LiteralKind::Int64(value) => Ok(Value::I64(value.clone())),
        LiteralKind::String(value) => Ok(Value::String(value.clone())),
    }
}

fn interpret_binary(node: &Binary, ctx: &Context) -> InterpretResult<Value> {
    let left = interpret_expression(node.left(), ctx)?;
    let right = interpret_expression(node.right(), ctx)?;

    match (left, node.op(), right) {
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

fn interpret_call(node: &Call, ctx: &Context) -> InterpretResult<Value> {
    let head = interpret_expression(node.head(), ctx)?;
    let args = interpret_consecutive(node.args(), interpret_expression, ctx)?;

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

fn interpret_member_access(node: &MemberAccess, ctx: &Context) -> InterpretResult<Value> {
    let head = interpret_expression(node.root(), ctx)?;

    match head {
        Value::Object { name, members } => interpret_identifier(node.member(), &members),
        Value::Module { name, members } => interpret_identifier(node.member(), &members),
        head => Err(format!("Value has not accessable members: {head:?}")),
    }
}

fn interpret_identifier(node: &Identifier, ctx: &Context) -> InterpretResult<Value> {
    Ok(ctx.get(node.value()).unwrap().clone())
}

fn interpret_expression(expression: &Expression, ctx: &Context) -> InterpretResult<Value> {
    match expression {
        Expression::Literal(node) => interpret_literal(node),
        Expression::Binary(node) => interpret_binary(node, ctx),
        Expression::Call(node) => interpret_call(node, ctx),
        Expression::MemberAccess(node) => interpret_member_access(node, ctx),
        Expression::Identifier(node) => interpret_identifier(node, ctx),
    }
}

fn interpret_import_path(node: &ImportPath, ctx: &Context) -> InterpretResult<(String, Value)> {
    match node {
        ImportPath::Simple(node) => {
            Ok((node.value().to_string(), interpret_identifier(node, ctx)?))
        }
        ImportPath::Complex { root, member } => {
            let (root_name, root_value) = interpret_import_path(root, ctx)?;

            Ok((
                format!("{root_name}.{}", member.value().to_string()),
                match root_value {
                    Value::Object { name, members } => interpret_identifier(member, &members),
                    Value::Module { name, members } => interpret_identifier(member, &members),
                    root_value => Err(format!("Value has not accessable members: {root_value:?}")),
                }?,
            ))
        }
    }
}

fn interpret_import(node: &Import, ctx: &mut Context) -> InterpretResult<Value> {
    if let Some(from_path) = node.from() {
        let (_, from_value) = interpret_import_path(from_path, ctx)?;
        let (name, value) = match from_value {
            Value::Object { name: _, members } => interpret_import_path(node.path(), &members),
            Value::Module { name: _, members } => interpret_import_path(node.path(), &members),
            head => Err(format!("Value has not accessable members: {head:?}")),
        }?;

        ctx.insert(name, value.clone());
        Ok(value)
    } else {
        let (path, value) = interpret_import_path(node.path(), ctx)?;
        ctx.insert(path, value);

        Ok(Value::Unit)
    }
}

fn interpret_statement(node: &Statement, ctx: &mut Context) -> InterpretResult<Value> {
    match node {
        Statement::Expression(node) => interpret_expression(node, ctx),
        Statement::Import(node) => interpret_import(node, ctx),
    }
}

pub fn interpret_script(script: &Script, ctx: &Context) -> InterpretResult<Value> {
    let mut last = Value::Unit;
    let mut ctx = ctx.clone();

    for statement in script.statements() {
        last = interpret_statement(statement, &mut ctx)?;
    }

    Ok(last)
}
