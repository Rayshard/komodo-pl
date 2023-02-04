pub mod import;
pub mod import_path;

use serde::Serialize;

use crate::compiler::{
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

use self::import::Import;

use super::{expression::Expression, Node};

#[derive(Serialize)]
pub enum Statement<'source> {
    Import(Import<'source>),
    Expression(Expression<'source>),
}

impl<'source> Node for Statement<'source> {
    fn ts_type(&self) -> &TSType {
        &TSType::Unit
    }

    fn range(&self) -> &Range {
        match self {
            Statement::Import(import) => import.range(),
            Statement::Expression(expression) => expression.range(),
        }
    }

    fn source(&self) -> &TextSource {
        match self {
            Statement::Import(import) => import.source(),
            Statement::Expression(expression) => expression.source(),
        }
    }
}
