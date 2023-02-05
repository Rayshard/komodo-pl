use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::location::Location};

#[derive(Serialize)]
pub enum LiteralKind {
    Int64(i64),
    String(String),
}

#[derive(Serialize)]
pub struct Literal<'source> {
    kind: LiteralKind,
    ts_type: TSType,
    location: Location<'source>,
}

impl<'source> Literal<'source> {
    pub fn new(kind: LiteralKind, ts_type: TSType, location: Location<'source>) -> Self {
        Self {
            kind,
            ts_type,
            location,
        }
    }

    pub fn kind(&self) -> &LiteralKind {
        &self.kind
    }
}

impl<'source> Node<'source> for Literal<'source> {
    fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    fn location(&self) -> Location<'source> {
        self.location.clone()
    }
}
