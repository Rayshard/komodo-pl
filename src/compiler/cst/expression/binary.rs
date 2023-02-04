use serde::Serialize;

use crate::compiler::{
    cst::Node,
    utilities::{range::Range, text_source::TextSource},
};

use super::{binary_operator::BinaryOperator, Expression};

#[derive(Serialize)]
pub struct Binary<'source> {
    left: Box<Expression<'source>>,
    op: BinaryOperator<'source>,
    right: Box<Expression<'source>>,
}

impl<'source> Node<'source> for Binary<'source> {
    fn range(&self) -> Range {
        Range::new(self.left.range().start(), self.right.range().end())
    }

    fn source(&self) -> &'source TextSource {
        self.op.source()
    }
}
