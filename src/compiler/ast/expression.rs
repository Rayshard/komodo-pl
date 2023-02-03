use serde::Serialize;

use crate::compiler::{parsing::cst::binary_operator::BinaryOperatorKind};

use super::{ExpressionNode, LiteralNode};

#[derive(Serialize)]
pub enum Expression<'source> {
    Literal(LiteralNode<'source>),
    Binary {
        left: Box<ExpressionNode<'source>>,
        op: BinaryOperatorKind,
        right: Box<ExpressionNode<'source>>,
    },
    Call {
        head: Box<ExpressionNode<'source>>,
        args: Vec<ExpressionNode<'source>>,
    },
    MemberAccess {
        head: Box<ExpressionNode<'source>>,
        member: String,
    },
    Identifier(String)
}
