use serde::Serialize;

use super::{ImportPathNode, ExpressionNode};

#[derive(Serialize)]
pub enum Statement<'source> {
    Import {
        import_path: ImportPathNode<'source>,
        from_path: Option<ImportPathNode<'source>>,
    },
    Expression(ExpressionNode<'source>),
}
