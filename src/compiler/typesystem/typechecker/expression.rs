use crate::compiler::{
    ast::{expression::{
        binary::Binary as ASTBinary,
        call::Call as ASTCall,
        identifier::Identifier as ASTIdentifier,
        literal::{Literal as ASTLiteral, LiteralKind as ASTLiteralKind},
        member_access::MemberAccess as ASTMemberAccess,
        Expression as ASTExpression,
    }, Node},
    cst::{
        expression::{
            binary::Binary as CSTBinary,
            call::Call as CSTCall,
            identifier::Identifier as CSTIdentifier,
            literal::{Literal as CSTLiteral, LiteralKind as CSTLiteralKind},
            member_access::MemberAccess as CSTMemberAccess,
            parenthesized::Parenthesized,
            Expression as CSTExpression,
        }, Node as CSTNode,
    },
    typesystem::{context::Context, ts_type::TSType},
};

use super::result::{TypecheckError, TypecheckErrorKind, TypecheckResult};

pub fn typecheck_identifier<'source>(
    node: &CSTIdentifier<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTIdentifier<'source>> {
    let name = node.value();
    let ts_type = ctx.get(name).map_err(|error| {
        TypecheckError::new(
            TypecheckErrorKind::Context(error),
            node.range().clone(),
            node.source(),
        )
    })?;

    Ok(ASTIdentifier {
        value: name.to_string(),
        ts_type: ts_type.clone(),
        source: node.source(),
        range: node.range().clone(),
    })
}

pub fn typecheck_member_access<'source>(
    node: &CSTMemberAccess<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTMemberAccess<'source>> {
    let root = typecheck(node.root(), ctx)?;
    let root_ctx = Context::from(root.ts_type(), None).map_err(|error| {
        TypecheckError::new(
            TypecheckErrorKind::Context(error),
            root.range().clone(),
            root.source(),
        )
    })?;
    let member = typecheck_identifier(node.member(), &root_ctx)?;

    Ok(ASTMemberAccess {
        root: Box::new(root),
        member,
    })
}

pub fn typecheck_literal<'source>(
    node: &CSTLiteral<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTLiteral<'source>> {
    match node.kind() {
        CSTLiteralKind::Integer => match node.token().value().parse::<i64>() {
            Ok(value) => Ok(ASTLiteral {
                kind: ASTLiteralKind::Int64(value),
                ts_type: TSType::Int64,
                range: node.range().clone(),
                source: node.source(),
            }),
            Err(error) => Err(match error.kind() {
                std::num::IntErrorKind::PosOverflow | std::num::IntErrorKind::NegOverflow => {
                    TypecheckError::new(
                        TypecheckErrorKind::IntegerOverflow,
                        node.range().clone(),
                        node.source(),
                    )
                }
                error => panic!("Unexcpected error on {}: {error:?}", node.token().value()),
            }),
        },
        CSTLiteralKind::String => Ok(ASTLiteral {
            kind: ASTLiteralKind::String(node.token().value().trim_matches('"').to_string()),
            ts_type: TSType::String,
            range: node.range().clone(),
            source: node.source(),
        }),
    }
}

pub fn typecheck_call<'source>(
    node: &CSTCall<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTCall<'source>> {
    // let head = typecheck(head, ctx)?;
    //         let args = typecheck_consecutive(&args[..], typecheck, ctx)?;

    //         let return_type = match head.ts_type() {
    //             TSType::Function {
    //                 name: _,
    //                 parameters,
    //                 return_type,
    //             } => {
    //                 if args.len() != parameters.len() {
    //                     return Err(TypecheckError::new(
    //                         TypecheckErrorKind::NotEnoughArguments {
    //                             expected: parameters.len(),
    //                             found: args.len(),
    //                         },
    //                         Range::new(
    //                             open_parenthesis.range().start(),
    //                             close_parenthesis.range().end(),
    //                         ),
    //                         expression.source(),
    //                     ));
    //                 }

    //                 for (arg, (_, parameter)) in args.iter().zip(parameters) {
    //                     if arg.ts_type() != parameter {
    //                         return Err(TypecheckError::new(
    //                             TypecheckErrorKind::Unexpected {
    //                                 expected: parameter.clone(),
    //                                 found: arg.ts_type().clone(),
    //                             },
    //                             arg.range().clone(),
    //                             arg.source(),
    //                         ));
    //                     }
    //                 }

    //                 return_type.as_ref().clone()
    //             }
    //             ts_type => {
    //                 return Err(TypecheckError::new(
    //                     TypecheckErrorKind::TypeIsNotCallable(ts_type.clone()),
    //                     head.range().clone(),
    //                     head.source(),
    //                 ))
    //             }
    //         };

    //         Ok(ExpressionNode::new(
    //             ASTExpression::Call {
    //                 head: Box::new(head),
    //                 args,
    //             },
    //             return_type,
    //             expression.source(),
    //             expression.range(),
    //         ))
    todo!()
}

pub fn typecheck_binary<'source>(
    node: &CSTBinary<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTBinary<'source>> {
    // let left = typecheck(left.as_ref(), ctx)?;
    //         let right = typecheck(right.as_ref(), ctx)?;

    //         match (left.ts_type(), op.kind(), right.ts_type()) {
    //             (TSType::Int64, op, TSType::Int64) => Ok(ExpressionNode::new(
    //                 ASTExpression::Binary {
    //                     left: Box::new(left),
    //                     op: op.clone(),
    //                     right: Box::new(right),
    //                 },
    //                 TSType::Int64,
    //                 expression.source(),
    //                 expression.range().clone(),
    //             )),
    //             (TSType::String, BinaryOperatorKind::Add, TSType::String) => {
    //                 Ok(ExpressionNode::new(
    //                     ASTExpression::Binary {
    //                         left: Box::new(left),
    //                         op: BinaryOperatorKind::Add,
    //                         right: Box::new(right),
    //                     },
    //                     TSType::String,
    //                     expression.source(),
    //                     expression.range().clone(),
    //                 ))
    //             }
    //             (left, op, right) => Err(TypecheckError::new(
    //                 TypecheckErrorKind::IncompatibleOperandsForBinaryOperator {
    //                     left: left.clone(),
    //                     op: op.clone(),
    //                     right: right.clone(),
    //                 },
    //                 expression.range(),
    //                 expression.source(),
    //             )),
    //         }
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
        CSTExpression::Unary(unary) => todo!(),
        CSTExpression::Binary(binary) => Ok(ASTExpression::Binary(typecheck_binary(binary, ctx)?)),
        CSTExpression::Parenthesized(parenthesized) => typecheck_parenthesized(parenthesized, ctx),
    }
}
