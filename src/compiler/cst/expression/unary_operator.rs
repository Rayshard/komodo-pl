use serde::Serialize;

use crate::compiler::utilities::{range::Range, text_source::TextSource};

use super::Node;

#[derive(Debug, Serialize)]
pub enum UnaryOperator {}

impl<'source> Node<'source> for UnaryOperator {
    fn range(&self) -> Range {
        todo!()
    }

    fn source(&self) -> &'source TextSource {
        todo!()
    }
}