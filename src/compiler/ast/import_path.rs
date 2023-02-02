use serde::Serialize;

use crate::compiler::utilities::range::Range;

use super::node::{Nodeable, Node};

#[derive(Serialize)]
pub enum ImportPath<'a> {
    Simple(String),
    Complex {
        head: Box<Node<'a, ImportPath<'a>>>,
        member: String,
    },
}


impl<'a> Nodeable for ImportPath<'a> {
    fn range(&self) -> Range {
        todo!()
    }
}