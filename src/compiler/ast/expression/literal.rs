use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::{range::Range, text_source::TextSource}};

#[derive(Serialize)]
pub enum LiteralKind {
    Int64(i64),
    String(String),
}

#[derive(Serialize)]
pub struct Literal<'source> {
    kind: LiteralKind,
    ts_type: TSType,
    range:Range,
    source: &'source TextSource,
}

impl<'source> Literal<'source> {
    pub fn kind(&self) -> &LiteralKind {
        &self.kind
    }
}

impl<'source> Node for Literal<'source> {
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