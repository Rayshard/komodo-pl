use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::location::Location};

#[derive(Serialize)]
pub struct Identifier<'source> {
    value: String,
    ts_type: TSType,
    location: Location<'source>,
}

impl<'source> Identifier<'source> {
    pub fn new(value: String, ts_type: TSType, location: Location<'source>) -> Self {
        Self {
            value,
            ts_type,
            location,
        }
    }

    pub fn value(&self) -> &str {
        &self.value
    }
}

impl<'source> Node<'source> for Identifier<'source> {
    fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    fn location(&self) -> Location<'source> {
        self.location.clone()
    }
}
