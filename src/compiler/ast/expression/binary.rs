use serde::Serialize;

use crate::compiler::{
    ast::Node,
    cst::expression::binary_operator::BinaryOperatorKind,
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

use super::Expression;

#[derive(Serialize)]
pub struct Binary<'source> {
    left: Box<Expression<'source>>,
    op: BinaryOperatorKind,
    right: Box<Expression<'source>>,
}

impl<'source> Binary<'source> {
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

impl<'source> Node for Binary<'source> {
    fn ts_type(&self) -> &TSType {
        todo!()
    }

    fn range(&self) -> &Range {
        todo!()
    }

    fn source(&self) -> &TextSource {
        todo!()
    }
}
