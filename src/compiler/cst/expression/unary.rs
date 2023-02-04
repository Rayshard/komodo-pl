use serde::Serialize;

use crate::compiler::{
    cst::Node,
    utilities::{range::Range, text_source::TextSource},
};

use super::{unary_operator::UnaryOperator, Expression};

#[derive(Serialize)]
pub struct Unary<'source> {
    operand: Box<Expression<'source>>,
    op: UnaryOperator,
}

impl<'source> Node<'source> for Unary<'source> {
    fn range(&self) -> Range {
        todo!()
    }

    fn source(&self) -> &'source TextSource {
        self.operand.source()
    }
}
