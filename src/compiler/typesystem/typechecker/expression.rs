use crate::compiler::{
    ast::{
        expression::{
            binary::Binary as ASTBinary,
            call::Call as ASTCall,
            identifier::Identifier as ASTIdentifier,
            literal::{Literal as ASTLiteral, LiteralKind as ASTLiteralKind},
            member_access::MemberAccess as ASTMemberAccess,
            Expression as ASTExpression,
        },
        Node,
    },
    cst::{
        expression::{
            binary::Binary as CSTBinary,
            call::Call as CSTCall,
            identifier::Identifier as CSTIdentifier,
            literal::{Literal as CSTLiteral, LiteralKind as CSTLiteralKind},
            member_access::MemberAccess as CSTMemberAccess,
            parenthesized::Parenthesized,
            Expression as CSTExpression,
        },
        Node as CSTNode,
    },
    typesystem::{context::Context, ts_type::TSType},
    utilities::{location::Location, range::Range},
};

use super::{
    result::{TypecheckError, TypecheckErrorKind, TypecheckResult},
    typecheck_consecutive,
};

pub fn typecheck_identifier<'source>(
    node: &CSTIdentifier<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTIdentifier<'source>> {
    let name = node.value();
    let ts_type = ctx.get(name, node.location().clone()).map_err(|error| {
        let location = error.location().clone();
        TypecheckError::new(TypecheckErrorKind::Context(error), location)
    })?;

    Ok(ASTIdentifier::new(
        name.to_string(),
        ts_type.clone(),
        node.location().clone(),
    ))
}

pub fn typecheck_member_access<'source>(
    node: &CSTMemberAccess<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTMemberAccess<'source>> {
    let root = typecheck(node.root(), ctx)?;
    let root_ctx = Context::from(root.ts_type(), None, root.location()).map_err(|error| {
        let location = error.location().clone();
        TypecheckError::new(TypecheckErrorKind::Context(error), location)
    })?;
    let member = typecheck_identifier(node.member(), &root_ctx)?;

    Ok(ASTMemberAccess::new(root, member))
}

pub fn typecheck_literal<'source>(
    node: &CSTLiteral<'source>,
    _ctx: &Context,
) -> TypecheckResult<'source, ASTLiteral<'source>> {
    match node.kind() {
        CSTLiteralKind::Integer => match node.token().value().parse::<i64>() {
            Ok(value) => Ok(ASTLiteral::new(
                ASTLiteralKind::Int64(value),
                TSType::Int64,
                node.location().clone(),
            )),
            Err(error) => Err(match error.kind() {
                std::num::IntErrorKind::PosOverflow | std::num::IntErrorKind::NegOverflow => {
                    TypecheckError::new(
                        TypecheckErrorKind::IntegerOverflow,
                        node.location().clone(),
                    )
                }
                error => panic!("Unexcpected error on {}: {error:?}", node.token().value()),
            }),
        },
        CSTLiteralKind::String => Ok(ASTLiteral::new(
            ASTLiteralKind::String(node.token().value().trim_matches('"').to_string()),
            TSType::String,
            node.location().clone(),
        )),
    }
}

pub fn typecheck_call<'source>(
    node: &CSTCall<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTCall<'source>> {
    let head = typecheck(node.head(), ctx)?;
    let args = typecheck_consecutive(&node.args(), typecheck, ctx)?;

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
                    Location::new(
                        node.location().source(),
                        Range::new(
                            node.open_parenthesis().location().range().start(),
                            node.close_parenthesis().location().range().end(),
                        ),
                    ),
                ));
            }

            for (arg, (_, parameter)) in args.iter().zip(parameters) {
                if arg.ts_type() != parameter {
                    return Err(TypecheckError::new(
                        TypecheckErrorKind::Unexpected {
                            expected: parameter.clone(),
                            found: arg.ts_type().clone(),
                        },
                        arg.location().clone(),
                    ));
                }
            }

            return_type.as_ref().clone()
        }
        ts_type => {
            return Err(TypecheckError::new(
                TypecheckErrorKind::TypeIsNotCallable(ts_type.clone()),
                head.location().clone(),
            ))
        }
    };

    Ok(ASTCall::new(head, args, return_type))
}

pub fn typecheck_binary<'source>(
    _node: &CSTBinary<'source>,
    _ctx: &Context,
) -> TypecheckResult<'source, ASTBinary<'source>> {
    // let left = typecheck(node.left(), ctx)?;
    // let right = typecheck(node.right(), ctx)?;

    // let ts_type = match (left.ts_type(), node.op().kind(), right.ts_type()) {
    //     (TSType::Int64, BinaryOperatorKind::Add, TSType::Int64) => TSType::Int64,
    //     (TSType::String, BinaryOperatorKind::Add, TSType::String) => TSType::String,
    //     (left, op, right) => return Err(TypecheckError::new(
    //         TypecheckErrorKind::IncompatibleOperandsForBinaryOperator {
    //             left: left.clone(),
    //             op: op.clone(),
    //             right: right.clone(),
    //         },
    //         node.location().clone()
    //     )),
    // };

    // Ok(ASTBinary::new(left, node.op().clone(), right, ts_type))
    todo!()
}

pub fn typecheck_parenthesized<'source>(
    node: &Parenthesized<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTExpression<'source>> {
    typecheck(node.expression(), ctx)
}

pub fn typecheck<'source>(
    expression: &CSTExpression<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTExpression<'source>> {
    match expression {
        CSTExpression::Literal(literal) => {
            Ok(ASTExpression::Literal(typecheck_literal(literal, ctx)?))
        }
        CSTExpression::Identifier(identifier) => Ok(ASTExpression::Identifier(
            typecheck_identifier(identifier, ctx)?,
        )),
        CSTExpression::MemberAccess(member_access) => Ok(ASTExpression::MemberAccess(
            typecheck_member_access(member_access, ctx)?,
        )),
        CSTExpression::Call(call) => Ok(ASTExpression::Call(typecheck_call(call, ctx)?)),
        CSTExpression::Unary(_) => todo!(),
        CSTExpression::Binary(binary) => Ok(ASTExpression::Binary(typecheck_binary(binary, ctx)?)),
        CSTExpression::Parenthesized(parenthesized) => typecheck_parenthesized(parenthesized, ctx),
    }
}
