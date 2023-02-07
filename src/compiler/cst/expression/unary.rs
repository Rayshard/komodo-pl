use serde::Serialize;

use crate::compiler::{cst::Node, utilities::location::Location};

use super::{unary_operator::UnaryOperator, Expression};

#[derive(Serialize, Clone)]
pub struct Unary<'source> {
    operand: Box<Expression<'source>>,
    op: UnaryOperator,
}

impl<'source> Node<'source> for Unary<'source> {
    fn location(&self) -> Location<'source> {
        todo!()
    }
}
