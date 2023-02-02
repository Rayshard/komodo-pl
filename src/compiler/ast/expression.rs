use serde::Serialize;

use crate::compiler::{parsing::cst::binary_operator::BinaryOperatorKind, utilities::range::Range};

use super::{literal::Literal, Node, node::Nodeable};

#[derive(Serialize)]
pub enum Expression<'a> {
    Literal(Node<'a, Literal>),
    Binary {
        left: Box<Node<'a, Expression<'a>>>,
        op: BinaryOperatorKind,
        right: Box<Node<'a, Expression<'a>>>,
    },
}

impl<'a> Nodeable for Expression<'a> {
    fn range(&self) -> Range {
        todo!()
    }
}
