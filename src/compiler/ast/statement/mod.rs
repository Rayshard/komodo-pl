pub mod import;
pub mod import_path;

use serde::Serialize;

use crate::compiler::{typesystem::ts_type::TSType, utilities::location::Location};

use self::import::Import;

use super::{expression::Expression, Node};

#[derive(Serialize, Clone)]
pub enum Statement<'source> {
    Import(Import<'source>),
    Expression(Expression<'source>),
}

impl<'source> Node<'source> for Statement<'source> {
    fn ts_type(&self) -> &TSType {
        &TSType::Unit
    }

    fn location(&self) -> Location<'source> {
        match self {
            Statement::Import(import) => import.location(),
            Statement::Expression(expression) => expression.location(),
        }
    }
}
