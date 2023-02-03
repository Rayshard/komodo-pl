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

use super::context::{Context, ContextError};

pub enum TypecheckErrorKind {
    Context(ContextError),
    IntegerOverflow,
    IncompatibleOperandsForBinaryOperator {
        left: TSType,
        op: BinaryOperatorKind,
        right: TSType,
    },
    ImportFromNonModule(TSType),
    TypeIsNotCallable(TSType),
    Unexpected {
        expected: TSType,
        found: TSType,
    },
    NotEnoughArguments {
        expected: usize,
        found: usize,
    },
}

pub struct TypecheckError<'source> {
    kind: TypecheckErrorKind,
    range: Range,
    source: &'source TextSource,
}

impl<'source> TypecheckError<'source> {
    pub fn new(
        kind: TypecheckErrorKind,
        range: Range,
        source: &'source TextSource,
    ) -> TypecheckError<'source> {
        TypecheckError {
            kind,
            range,
            source,
        }
    }
}

impl<'source> ToString for TypecheckError<'source> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            match &self.kind {
                TypecheckErrorKind::Context(error) => error.clone(),
                TypecheckErrorKind::IntegerOverflow =>
                    "Value outside of range for Int64".to_string(),
                TypecheckErrorKind::IncompatibleOperandsForBinaryOperator { left, op, right } =>
                    format!("Unable to apply operator {op:?} to {left:?} and {right:?}"),
                TypecheckErrorKind::ImportFromNonModule(ts_type) =>
                    format!("Expected module but found {ts_type:?}"),
                TypecheckErrorKind::TypeIsNotCallable(ts_type) =>
                    format!("Type {ts_type:?} is not callable"),
                TypecheckErrorKind::Unexpected { expected, found } =>
                    format!("Expected {expected:?} but found {found:?}"),
                TypecheckErrorKind::NotEnoughArguments { expected, found } =>
                    format!("Expected {expected} arguments but found {found}."),
            }
        )
    }
}

pub type TypecheckResult<'source, T> = Result<T, TypecheckError<'source>>;

type Typechecker<'source, TIn, TOut> =
    fn(item: &TIn, ctx: &Context) -> TypecheckResult<'source, TOut>;

type TypecheckerMut<'source, TIn, TOut> =
    fn(item: &TIn, ctx: &mut Context) -> TypecheckResult<'source, TOut>;

pub fn typecheck_consecutive<'source, TIn, TOut>(
    items: &[TIn],
    typechecker: Typechecker<'source, TIn, TOut>,
    ctx: &Context,
) -> TypecheckResult<'source, Vec<TOut>> {
    let mut results = vec![];

    for item in items {
        results.push(typechecker(item, ctx)?);
    }

    Ok(results)
}

pub fn typecheck_consecutive_mut<'source, TIn, TOut>(
    items: &[TIn],
    typechecker: TypecheckerMut<'source, TIn, TOut>,
    ctx: &mut Context,
) -> TypecheckResult<'source, Vec<TOut>> {
    let mut results = vec![];

    for item in items {
        results.push(typechecker(item, ctx)?);
    }

    Ok(results)
}

pub fn typecheck_expression<'source>(
    expression: &CSTExpression<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ExpressionNode<'source>> {
    match expression {
        CSTExpression::IntegerLiteral(token) => match token.value().parse::<i64>() {
            Ok(value) => Ok(ExpressionNode::new(
                ASTExpression::Literal(LiteralNode::new(
                    ASTLiteral::Int64(value),
                    TSType::Int64,
                    expression.source(),
                    expression.range().clone(),
                )),
                TSType::Int64,
                expression.source(),
                expression.range().clone(),
            )),
            Err(error) => Err(match error.kind() {
                std::num::IntErrorKind::PosOverflow | std::num::IntErrorKind::NegOverflow => {
                    TypecheckError::new(
                        TypecheckErrorKind::IntegerOverflow,
                        expression.range(),
                        expression.source(),
                    )
                }
                error => panic!("Unexcpected error on {}: {error:?}", token.value()),
            }),
        },
        CSTExpression::StringLiteral(token) => Ok(ExpressionNode::new(
            ASTExpression::Literal(LiteralNode::new(
                ASTLiteral::String(token.value().trim_matches('"').to_string()),
                TSType::String,
                expression.source(),
                expression.range().clone(),
            )),
            TSType::String,
            expression.source(),
            expression.range().clone(),
        )),
        CSTExpression::Identifier(token) => {
            let id = token.value();
            let ts_type = ctx.get(id).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    token.range().clone(),
                    token.source(),
                )
            })?;

            Ok(ExpressionNode::new(
                ASTExpression::Identifier(id.to_string()),
                ts_type.clone(),
                token.source(),
                token.range().clone(),
            ))
        }
        CSTExpression::MemberAccess {
            head,
            dot: _,
            member,
        } => {
            let head = typecheck_expression(head, ctx)?;
            let head_ctx = Context::from(head.ts_type(), None).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    head.range().clone(),
                    head.source(),
                )
            })?;

            let member_name = member.value();
            let ts_type = head_ctx.get(member_name).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    member.range().clone(),
                    member.source(),
                )
            })?;

            Ok(ExpressionNode::new(
                ASTExpression::MemberAccess {
                    head: Box::new(head),
                    member: member_name.to_string(),
                },
                ts_type.clone(),
                expression.source(),
                expression.range().clone(),
            ))
        }
        CSTExpression::Call {
            head,
            open_parenthesis,
            args,
            close_parenthesis,
        } => {
            let head = typecheck_expression(head, ctx)?;
            let args = typecheck_consecutive(&args[..], typecheck_expression, ctx)?;

            let return_type = match head.ts_type() {
                TSType::Function {
                    name: _,
                    parameters,
                    return_type,
                } => {
                    if args.len() != parameters.len() {
                        return Err(TypecheckError::new(
                            TypecheckErrorKind::NotEnoughArguments {
                                expected: parameters.len(),
                                found: args.len(),
                            },
                            Range::new(
                                open_parenthesis.range().start(),
                                close_parenthesis.range().end(),
                            ),
                            expression.source(),
                        ));
                    }

                    for (arg, (_, parameter)) in args.iter().zip(parameters) {
                        if arg.ts_type() != parameter {
                            return Err(TypecheckError::new(
                                TypecheckErrorKind::Unexpected {
                                    expected: parameter.clone(),
                                    found: arg.ts_type().clone(),
                                },
                                arg.range().clone(),
                                arg.source(),
                            ));
                        }
                    }

                    return_type.as_ref().clone()
                }
                ts_type => {
                    return Err(TypecheckError::new(
                        TypecheckErrorKind::TypeIsNotCallable(ts_type.clone()),
                        head.range().clone(),
                        head.source(),
                    ))
                }
            };

            Ok(ExpressionNode::new(
                ASTExpression::Call {
                    head: Box::new(head),
                    args,
                },
                return_type,
                expression.source(),
                expression.range(),
            ))
        }
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
                    expression.range().clone(),
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
                        expression.range().clone(),
                    ))
                }
                (left, op, right) => Err(TypecheckError::new(
                    TypecheckErrorKind::IncompatibleOperandsForBinaryOperator {
                        left: left.clone(),
                        op: op.clone(),
                        right: right.clone(),
                    },
                    expression.range(),
                    expression.source(),
                )),
            }
        }
        CSTExpression::Parenthesized {
            open_parenthesis: _,
            expression,
            close_parenthesis: _,
        } => typecheck_expression(expression, ctx),
    }
}

pub fn typecheck_import_path<'source>(
    path: &CSTImportPath<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ImportPathNode<'source>> {
    match path {
        CSTImportPath::Simple(token) => {
            let name = token.value();
            let ts_type = ctx.get(name).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    path.range().clone(),
                    path.source(),
                )
            })?;

            Ok(ImportPathNode::new(
                ASTImportPath::Simple(name.to_string()),
                ts_type.clone(),
                path.source(),
                path.range().clone(),
            ))
        }
        CSTImportPath::Complex {
            head,
            dot: _,
            member,
        } => {
            let head = typecheck_import_path(head, ctx)?;
            let import_ctx = Context::from(head.ts_type(), None).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    head.range().clone(),
                    head.source(),
                )
            })?;

            let name = member.value();
            let ts_type = import_ctx.get(name).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    member.range().clone(),
                    member.source(),
                )
            })?;

            Ok(ImportPathNode::new(
                ASTImportPath::Complex {
                    head: Box::new(head),
                    member: name.to_string(),
                },
                ts_type.clone(),
                path.source(),
                path.range().clone(),
            ))
        }
    }
}

pub fn typecheck_statement<'source>(
    statement: &CSTStatement<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, StatementNode<'source>> {
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
                    TSType::Module {
                        name: _,
                        members: _,
                    } => {
                        let mut import_ctx =
                            Context::from(from_path.ts_type(), None).map_err(|error| {
                                TypecheckError::new(
                                    TypecheckErrorKind::Context(error),
                                    from_path.range().clone(),
                                    from_path.source(),
                                )
                            })?;

                        Ok((
                            typecheck_import_path(import_path, &mut import_ctx)?,
                            Some(from_path),
                        ))
                    }
                    ts_type => Err(TypecheckError::new(
                        TypecheckErrorKind::ImportFromNonModule(ts_type.clone()),
                        from_path.range().clone(),
                        from_path.source(),
                    )),
                }
            } else {
                Ok((typecheck_import_path(import_path, ctx)?, None))
            }?;

            let name = match import_path.instance() {
                ASTImportPath::Simple(name) => name,
                ASTImportPath::Complex { head: _, member } => member,
            };

            ctx.set(name, import_path.ts_type().clone())
                .map_err(|error| {
                    TypecheckError::new(
                        TypecheckErrorKind::Context(error),
                        import_path.range().clone(),
                        import_path.source(),
                    )
                })?;

            Ok(StatementNode::new(
                ASTStatement::Import {
                    import_path,
                    from_path,
                },
                TSType::Unit,
                statement.source(),
                statement.range().clone(),
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
                statement.range().clone(),
            ))
        }
    }
}

pub fn typecheck_script<'source>(
    script: CSTScript<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ScriptNode<'source>> {
    let mut ctx = Context::new(Some(ctx));
    let statements = typecheck_consecutive_mut(script.statements(), typecheck_statement, &mut ctx)?;

    let ts_type = if let Some(statement) = statements.last() {
        statement.ts_type().clone()
    } else {
        TSType::Unit
    };

    Ok(ScriptNode::new(
        ASTScript::new(statements),
        ts_type,
        script.source(),
        script.range(),
    ))
}
