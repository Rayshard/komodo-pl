use serde::Serialize;

use crate::compiler::parsing::cst::binary_operator::BinaryOperatorKind;

use super::{literal::Literal, Node};

#[derive(Serialize)]
pub enum Expression<'a> {
    Literal(Node<'a, Literal>),
    Binary {
        left: Box<Node<'a, Expression<'a>>>,
        op: BinaryOperatorKind,
        right: Box<Node<'a, Expression<'a>>>,
    },
}
