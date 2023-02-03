use serde::Serialize;

use super::{ImportPathNode};

#[derive(Serialize)]
pub enum ImportPath<'source> {
    Simple(String),
    Complex {
        head: Box<ImportPathNode<'source>>,
        member: String,
    },
}
