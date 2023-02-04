use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::{range::Range, text_source::TextSource}};

use super::Expression;

#[derive(Serialize)]
pub struct Call<'source> {
    head: Box<Expression<'source>>,
    args: Vec<Expression<'source>>,
}

impl<'source> Call<'source> {
    pub fn head(&self) -> &Expression<'source> {
        &self.head
    }

    pub fn args(&self) -> &[Expression<'source>] {
        &self.args
    }
}

impl<'source> Node for Call<'source> {
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