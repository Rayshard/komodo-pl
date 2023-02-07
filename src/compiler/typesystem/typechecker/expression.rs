use crate::compiler::{
    ast::{
        expression::{
            binary::Binary as ASTBinary,
            binary_operator::BinaryOperator,
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
    typesystem::{
        context::Context,
        ts_type::{Function as TSFunction, FunctionOverload, TSType},
    },
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
    let ts_type = ctx.get(name, node.location()).map_err(|error| {
        let location = error.location().clone();
        TypecheckError::new(TypecheckErrorKind::Context(error), location)
    })?;

    Ok(ASTIdentifier::new(
        name.to_string(),
        ts_type.clone(),
        node.location(),
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
                node.location(),
            )),
            Err(error) => Err(match error.kind() {
                std::num::IntErrorKind::PosOverflow | std::num::IntErrorKind::NegOverflow => {
                    TypecheckError::new(TypecheckErrorKind::IntegerOverflow, node.location())
                }
                error => panic!("Unexcpected error on {}: {error:?}", node.token().value()),
            }),
        },
        CSTLiteralKind::String => Ok(ASTLiteral::new(
            ASTLiteralKind::String(node.token().value().trim_matches('"').to_string()),
            TSType::String,
            node.location(),
        )),
    }
}

pub fn typecheck_args_against_overload<'source>(
    args: &[ASTExpression<'source>],
    overload: &FunctionOverload,
    location: Location<'source>,
) -> TypecheckResult<'source, ()> {
    if args.len() != overload.parameters().len() {
        return Err(TypecheckError::new(
            TypecheckErrorKind::NotEnoughArguments {
                expected: overload.parameters().len(),
                found: args.len(),
            },
            location,
        ));
    }

    for (arg, (_, parameter)) in args.iter().zip(overload.parameters()) {
        if arg.ts_type() != parameter {
            return Err(TypecheckError::new(
                TypecheckErrorKind::Unexpected {
                    expected: parameter.clone(),
                    found: arg.ts_type().clone(),
                },
                arg.location(),
            ));
        }
    }

    Ok(())
}

pub fn typecheck_args_against_function<'source>(
    args: &[ASTExpression<'source>],
    function: &TSFunction,
    args_location: Location<'source>,
) -> TypecheckResult<'source, TSType> {
    for overload in function.overloads() {
        if let Ok(()) = typecheck_args_against_overload(args, &overload, args_location.clone()) {
            return Ok(overload.return_type().clone());
        }
    }

    Err(TypecheckError::new(
        TypecheckErrorKind::NoOverloadMatchesArguments {
            function: function.clone(),
            args: args.iter().map(|arg| arg.ts_type().clone()).collect(),
        },
        args_location,
    ))
}

pub fn typecheck_call<'source>(
    node: &CSTCall<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTCall<'source>> {
    let head = typecheck(node.head(), ctx)?;
    let args = typecheck_consecutive(&node.args(), typecheck, ctx)?;

    let return_type = match head.ts_type() {
        TSType::Function(function) => {
            let args_location = Location::new(
                node.location().source(),
                Range::new(
                    node.open_parenthesis().location().range().start(),
                    node.close_parenthesis().location().range().end(),
                ),
            );

            if let [overload] = function.overloads() {
                typecheck_args_against_overload(&args, &overload, args_location)?;

                overload.return_type().clone()
            } else {
                let mut return_type = None;

                for overload in function.overloads() {
                    if let Ok(()) =
                        typecheck_args_against_overload(&args, &overload, args_location.clone())
                    {
                        return_type = Some(overload.return_type().clone());
                        break;
                    }
                }

                match return_type {
                    Some(return_type) => return_type,
                    None => {
                        return Err(TypecheckError::new(
                            TypecheckErrorKind::NoOverloadMatchesArguments {
                                function: function.clone(),
                                args: args.iter().map(|arg| arg.ts_type().clone()).collect(),
                            },
                            args_location,
                        ))
                    }
                }
            }
        }
        ts_type => {
            return Err(TypecheckError::new(
                TypecheckErrorKind::TypeIsNotCallable(ts_type.clone()),
                head.location(),
            ))
        }
    };

    Ok(ASTCall::new(head, args, return_type))
}

pub fn typecheck_binary<'source>(
    node: &CSTBinary<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTBinary<'source>> {
    let left = typecheck(node.left(), ctx)?;
    let right = typecheck(node.right(), ctx)?;
    let op = ctx.get_operator_function(node.op()).map_err(|error| {
        let location = error.location().clone();
        TypecheckError::new(TypecheckErrorKind::Context(error), location)
    })?;
    let return_type =
        typecheck_args_against_function(&[left.clone(), right.clone()], op, node.location())?;

    Ok(ASTBinary::new(
        left,
        BinaryOperator::new(
            node.op().kind().clone(),
            TSType::Function(op.clone()),
            node.op().location(),
        ),
        right,
        return_type,
    ))
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
