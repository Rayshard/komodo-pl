use serde::Serialize;

use crate::compiler::{
    cst::Node,
    utilities::{location::Location, range::Range},
};

use super::{binary_operator::BinaryOperator, Expression};

#[derive(Serialize, Clone)]
pub struct Binary<'source> {
    left: Box<Expression<'source>>,
    op: BinaryOperator<'source>,
    right: Box<Expression<'source>>,
    location: Location<'source>,
}

impl<'source> Binary<'source> {
    pub fn new(
        left: Expression<'source>,
        op: BinaryOperator<'source>,
        right: Expression<'source>,
    ) -> Self {
        let location = Location::new(
            op.location().source(),
            Range::new(
                left.location().range().start(),
                right.location().range().end(),
            ),
        );

        Self {
            left: Box::new(left),
            op: op,
            right: Box::new(right),
            location,
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
    fn location(&self) -> &Location<'source> {
        &self.location
    }
}
