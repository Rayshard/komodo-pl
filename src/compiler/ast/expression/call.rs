use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::location::Location};

use super::Expression;

#[derive(Serialize)]
pub struct Call<'source> {
    head: Box<Expression<'source>>,
    args: Vec<Expression<'source>>,
    ts_type: TSType,
}

impl<'source> Call<'source> {
    pub fn new(head: Expression<'source>, args: Vec<Expression<'source>>, ts_type: TSType) -> Self {
        Self {
            head: Box::new(head),
            args,
            ts_type,
        }
    }

    pub fn head(&self) -> &Expression<'source> {
        &self.head
    }

    pub fn args(&self) -> &[Expression<'source>] {
        &self.args
    }
}

impl<'source> Node<'source> for Call<'source> {
    fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    fn location(&self) -> Location<'source> {
        todo!()
    }
}
