use serde::Serialize;

use crate::compiler::utilities::range::Range;

use super::{node::Nodeable, ImportPathNode, ExpressionNode};

#[derive(Serialize)]
pub enum Statement<'source> {
    Import {
        import_path: ImportPathNode<'source>,
        from_path: Option<ImportPathNode<'source>>,
    },
    Expression(ExpressionNode<'source>),
}

impl<'source> Nodeable for Statement<'source> {
    fn range(&self) -> Range {
        todo!()
    }
}
