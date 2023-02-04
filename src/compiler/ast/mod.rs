use serde::Serialize;

use super::{utilities::{range::Range, text_source::TextSource}, typesystem::ts_type::TSType};

pub mod expression;
pub mod script;
pub mod statement;

pub trait Node : Serialize {
    fn ts_type(&self) -> &TSType;
    fn range(&self) -> &Range;
    fn source(&self) -> &TextSource;
}
