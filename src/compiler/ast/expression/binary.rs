use serde::Serialize;

use crate::compiler::{
    ast::Node,
    cst::expression::binary_operator::BinaryOperatorKind,
    typesystem::ts_type::TSType,
    utilities::{location::Location, range::Range},
};

use super::{binary_operator::BinaryOperator, Expression};

#[derive(Serialize)]
pub struct Binary<'source> {
    left: Box<Expression<'source>>,
    op: BinaryOperator<'source>,
    right: Box<Expression<'source>>,
    ts_type: TSType,
}

impl<'source> Binary<'source> {
    pub fn new(
        left: Expression<'source>,
        op: BinaryOperator<'source>,
        right: Expression<'source>,
        ts_type: TSType,
    ) -> Self {
        Self {
            left: Box::new(left),
            op,
            right: Box::new(right),
            ts_type,
        }
    }

    pub fn left(&self) -> &Expression<'source> {
        self.left.as_ref()
    }

    pub fn op(&self) -> &BinaryOperator<'source> {
        &self.op
    }

    pub fn right(&self) -> &Expression<'source> {
        self.right.as_ref()
    }
}

impl<'source> Node<'source> for Binary<'source> {
    fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    fn location(&self) -> Location<'source> {
        Location::new(
            self.op.location().source(),
            Range::new(
                self.left.location().range().start(),
                self.right.location().range().end(),
            ),
        )
    }
}
