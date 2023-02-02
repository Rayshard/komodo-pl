use crate::compiler::{
    ast::{
        expression::Expression as ASTExpression, import_path::ImportPath as ASTImportPath,
        literal::Literal as ASTLiteral, script::Script as ASTScript,
        statement::Statement as ASTStatement, ExpressionNode, ImportPathNode, LiteralNode,
        ScriptNode, StatementNode,
    },
    parsing::cst::{
        binary_operator::BinaryOperatorKind,
        expression::Expression as CSTExpression,
        script::Script as CSTScript,
        statement::{ImportPath as CSTImportPath, Statement as CSTStatement},
        Node as CSTNode,
    },
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

use super::context::Context;

#[derive(Debug)]
pub struct TypecheckError<'a> {
    range: Range,
    message: String,
    source: &'a TextSource,
}

impl<'a> ToString for TypecheckError<'a> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            self.message
        )
    }
}

pub type TypecheckResult<'a, T> = Result<T, TypecheckError<'a>>;

pub fn typecheck_expression<'a>(
    expression: &CSTExpression<'a>,
    ctx: &mut Context,
) -> TypecheckResult<'a, ExpressionNode<'a>> {
    match expression {
        CSTExpression::IntegerLiteral(token) => match token.value().parse::<i64>() {
            Ok(value) => Ok(ExpressionNode::new(
                ASTExpression::Literal(LiteralNode::new(
                    ASTLiteral::Int64(value),
                    TSType::Int64,
                    expression.source(),
                )),
                TSType::Int64,
                expression.source(),
            )),
            Err(error) => Err(TypecheckError {
                range: expression.range(),
                message: match error.kind() {
                    std::num::IntErrorKind::PosOverflow | std::num::IntErrorKind::NegOverflow => {
                        "Value outside of range for Int64".to_string()
                    }
                    error => panic!("Unexcpected error on {}: {error:?}", token.value()),
                },
                source: expression.source(),
            }),
        },
        CSTExpression::StringLiteral(token) => Ok(ExpressionNode::new(
            ASTExpression::Literal(LiteralNode::new(
                ASTLiteral::String(token.value().trim_matches('"').to_string()),
                TSType::String,
                expression.source(),
            )),
            TSType::String,
            expression.source(),
        )),
        CSTExpression::Identifier(_) => todo!(),
        CSTExpression::MemberAccess {
            head: _,
            dot: _,
            member: _,
        } => todo!(),
        CSTExpression::Call {
            head: _,
            open_parenthesis: _,
            arg: _,
            close_parenthesis: _,
        } => todo!(),
        CSTExpression::Unary { operand: _, op: _ } => todo!(),
        CSTExpression::Binary { left, op, right } => {
            let left = typecheck_expression(left.as_ref(), ctx)?;
            let right = typecheck_expression(right.as_ref(), ctx)?;

            match (left.ts_type(), op.kind(), right.ts_type()) {
                (TSType::Int64, op, TSType::Int64) => Ok(ExpressionNode::new(
                    ASTExpression::Binary {
                        left: Box::new(left),
                        op: op.clone(),
                        right: Box::new(right),
                    },
                    TSType::Int64,
                    expression.source(),
                )),
                (TSType::String, BinaryOperatorKind::Add, TSType::String) => {
                    Ok(ExpressionNode::new(
                        ASTExpression::Binary {
                            left: Box::new(left),
                            op: BinaryOperatorKind::Add,
                            right: Box::new(right),
                        },
                        TSType::String,
                        expression.source(),
                    ))
                }
                (left, op, right) => Err(TypecheckError {
                    range: expression.range(),
                    message: format!("Unable to apply operator {op:?} to {left:?} and {right:?}"),
                    source: expression.source(),
                }),
            }
        }
        CSTExpression::Parenthesized {
            open_parenthesis: _,
            expression,
            close_parenthesis: _,
        } => typecheck_expression(expression, ctx),
    }
}

pub fn typecheck_import_path<'a, 'b>(
    path: &CSTImportPath<'a>,
    ctx: &mut Context<'b>,
) -> TypecheckResult<'a, ImportPathNode<'a>> {
    match path {
        CSTImportPath::Simple(token) => {
            let name = token.value();
            let ts_type = ctx.get(name).map_or_else(
                |error| {
                    Err(TypecheckError {
                        range: path.range().clone(),
                        message: error,
                        source: path.source(),
                    })
                },
                |ts_type| Ok(ts_type.clone()),
            )?;

            Ok(ImportPathNode::new(
                ASTImportPath::Simple(name.to_string()),
                ts_type,
                path.source(),
            ))
        }
        CSTImportPath::Complex {
            head,
            dot: _,
            member,
        } => {
            let head = typecheck_import_path(head, ctx)?;
            let import_ctx = Context::new(None); // TODO: ctx needs to be created from head

            let name = member.value();
            let ts_type = import_ctx.get(name).map_or_else(
                |error| {
                    Err(TypecheckError {
                        range: path.range().clone(),
                        message: error,
                        source: path.source(),
                    })
                },
                |ts_type| Ok(ts_type.clone()),
            )?;

            Ok(ImportPathNode::new(
                ASTImportPath::Complex {
                    head: Box::new(head),
                    member: name.to_string(),
                },
                ts_type,
                path.source(),
            ))
        }
    }
}

pub fn typecheck_statement<'a>(
    statement: &CSTStatement<'a>,
    ctx: &mut Context,
) -> TypecheckResult<'a, StatementNode<'a>> {
    match statement {
        CSTStatement::Import {
            keyword_import: _,
            import_path,
            from_path,
            semicolon: _,
        } => {
            let (import_path, from_path) = if let Some((_, from_path)) = from_path {
                let from_path = typecheck_import_path(from_path, ctx)?;

                match from_path.ts_type() {
                    TSType::Module { name, members } => {
                        let mut import_ctx = Context::new(None); // TODO: ctx needs ot be created from module

                        (
                            typecheck_import_path(import_path, &mut import_ctx)?,
                            Some(from_path),
                        )
                    }
                    ts_type => {
                        return Err(TypecheckError {
                            range: from_path.range(),
                            message: format!("Expected module but found {ts_type:?}"),
                            source: from_path.source(),
                        })
                    }
                }
            } else {
                (typecheck_import_path(import_path, ctx)?, None)
            };

            let name = match import_path.instance() {
                ASTImportPath::Simple(name) => name,
                ASTImportPath::Complex { head: _, member } => member,
            };

            ctx.set(name, import_path.ts_type().clone()).map_or_else(
                |error| {
                    Err(TypecheckError {
                        range: import_path.range(),
                        message: error,
                        source: import_path.source(),
                    })
                },
                |ts_type| Ok(ts_type.clone()),
            )?;

            Ok(StatementNode::new(
                ASTStatement::Import {
                    import_path,
                    from_path,
                },
                TSType::Unit,
                statement.source(),
            ))
        }
        CSTStatement::Expression {
            expression,
            semicolon: _,
        } => {
            let expression = typecheck_expression(expression, ctx)?;
            Ok(StatementNode::new(
                ASTStatement::Expression(expression),
                TSType::Unit,
                statement.source(),
            ))
        }
    }
}

pub fn typecheck_script<'a>(script: CSTScript<'a>) -> TypecheckResult<'a, ScriptNode<'a>> {
    let mut ctx = Context::new(None);
    let mut statements = vec![];

    for statement in script.statements() {
        let statement = typecheck_statement(statement, &mut ctx)?;
        statements.push(statement);
    }

    let ts_type = if let Some(statement) = statements.last() {
        statement.ts_type().clone()
    } else {
        TSType::Unit
    };

    Ok(ScriptNode::new(
        ASTScript::new(statements),
        ts_type,
        script.source(),
    ))
}
