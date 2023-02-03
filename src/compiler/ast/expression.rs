use serde::Serialize;

use crate::compiler::{parsing::cst::binary_operator::BinaryOperatorKind, utilities::range::Range};

use super::{node::Nodeable, LiteralNode, ExpressionNode};

#[derive(Serialize)]
pub enum Expression<'source> {
    Literal(LiteralNode<'source>),
    Binary {
        left: Box<ExpressionNode<'source>>,
        op: BinaryOperatorKind,
        right: Box<ExpressionNode<'source>>,
    },
}

impl<'source> Nodeable for Expression<'source> {
    fn range(&self) -> Range {
        todo!()
    }
}
