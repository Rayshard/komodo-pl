use serde::Serialize;

use super::{typesystem::ts_type::TSType, utilities::location::Location};

pub mod expression;
pub mod script;
pub mod statement;

pub trait Node<'source>: Serialize {
    fn ts_type(&self) -> &TSType;
    fn location(&self) -> Location<'source>;
}
