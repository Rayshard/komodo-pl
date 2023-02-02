use serde::Serialize;

use crate::compiler::utilities::range::Range;

use super::node::Nodeable;

#[derive(Serialize)]
pub enum Literal {
    Int64(i64),
    String(String),
}

impl Nodeable for Literal {
    fn range(&self) -> Range {
        todo!()
    }
}