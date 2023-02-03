use serde::Serialize;

use crate::compiler::utilities::range::Range;

use super::{node::Nodeable, ImportPathNode};

#[derive(Serialize)]
pub enum ImportPath<'source> {
    Simple(String),
    Complex {
        head: Box<ImportPathNode<'source>>,
        member: String,
    },
}

impl<'source> Nodeable for ImportPath<'source> {
    fn range(&self) -> Range {
        todo!()
    }
}
