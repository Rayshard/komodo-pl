use serde::Serialize;

use crate::compiler::{
    ast::Node, cst::expression::binary_operator::BinaryOperatorKind, typesystem::ts_type::TSType,
    utilities::location::Location,
};

use super::Expression;

#[derive(Serialize)]
pub struct Binary<'source> {
    left: Box<Expression<'source>>,
    op: BinaryOperatorKind,
    right: Box<Expression<'source>>,
}

impl<'source> Binary<'source> {
    pub fn new(
        left: Expression<'source>,
        op: BinaryOperatorKind,
        right: Expression<'source>,
    ) -> Self {
        Self {
            left: Box::new(left),
            op,
            right: Box::new(right),
        }
    }

    pub fn left(&self) -> &Expression<'source> {
        self.left.as_ref()
    }

    pub fn op(&self) -> &BinaryOperatorKind {
        &self.op
    }

    pub fn right(&self) -> &Expression<'source> {
        self.right.as_ref()
    }
}

impl<'source> Node<'source> for Binary<'source> {
    fn ts_type(&self) -> &TSType {
        todo!()
    }

    fn location(&self) -> Location<'source> {
        todo!()
    }
}
