use serde::Serialize;

use crate::compiler::{utilities::{text_source::TextSource, range::Range}, typesystem::ts_type::TSType, ast::Node};

#[derive(Serialize)]
pub struct Identifier<'source> {
    value: String,
    ts_type: TSType,
    source: &'source TextSource,
    range: Range
}

impl<'source> Identifier<'source> {
    pub fn value(&self) -> &str {
        &self.value
    }
}

impl<'source> Node for Identifier<'source> {
    fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    fn range(&self) -> &Range {
        &self.range
    }

    fn source(&self) -> &TextSource {
        self.source
    }
}