use serde::Serialize;

use crate::compiler::utilities::range::Range;

use super::{expression::Expression, import_path::ImportPath, node::Nodeable, Node};

#[derive(Serialize)]
pub enum Statement<'a> {
    Import {
        import_path: Node<'a, ImportPath<'a>>,
        from_path: Option<Node<'a, ImportPath<'a>>>,
    },
    Expression(Node<'a, Expression<'a>>),
}

impl<'a> Nodeable for Statement<'a> {
    fn range(&self) -> Range {
        todo!()
    }
}
