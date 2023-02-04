use crate::compiler::{
    parsing::cst::{expression::Expression as CSTExpression, Node, binary_operator::BinaryOperatorKind},
    typesystem::{context::Context, ts_type::TSType}, ast::{expression::Expression as ASTExpression, literal::Literal as ASTLiteral, ExpressionNode, LiteralNode}, utilities::range::Range,
};

use super::{result::{TypecheckError, TypecheckErrorKind, TypecheckResult}, typecheck_consecutive};

pub fn typecheck<'source>(
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
            let head = typecheck(head, ctx)?;
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
            let head = typecheck(head, ctx)?;
            let args = typecheck_consecutive(&args[..], typecheck, ctx)?;

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
            let left = typecheck(left.as_ref(), ctx)?;
            let right = typecheck(right.as_ref(), ctx)?;

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
        } => typecheck(expression, ctx),
    }
}
