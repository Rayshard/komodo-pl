use serde::Serialize;

use crate::compiler::{typesystem::ts_type::TSType, utilities::location::Location, cst::expression::binary_operator::BinaryOperatorKind, ast::Node};

#[derive(Serialize, Clone)]
pub struct BinaryOperator<'source> {
    kind: BinaryOperatorKind,
    ts_type: TSType,
    location: Location<'source>,
}

impl<'source> BinaryOperator<'source> {
    pub fn new(kind: BinaryOperatorKind, ts_type: TSType, location: Location<'source>) -> Self {
        Self {
            kind,
            ts_type,
            location,
        }
    }

    pub fn kind(&self) -> &BinaryOperatorKind {
        &self.kind
    }
}

impl<'source> Node<'source> for BinaryOperator<'source> {
    fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    fn location(&self) -> Location<'source> {
        self.location.clone()
    }
}

