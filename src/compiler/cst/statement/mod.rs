pub mod import;
pub mod import_path;

use serde::Serialize;

use crate::compiler::{
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

use self::import::Import;

use super::{expression::Expression, Node};

#[derive(Serialize, Clone)]
pub enum StatementKind<'source> {
    Import(Import<'source>),
    Expression(Expression<'source>),
}

#[derive(Serialize, Clone)]
pub struct Statement<'source> {
    kind: StatementKind<'source>,
    semicolon: Token<'source>,
}

impl<'source> Statement<'source> {
    pub fn new(kind: StatementKind<'source>, semicolon: Token<'source>) -> Self {
        Self {
            kind,
            semicolon,
        }
    }

    pub fn kind(&self) -> &StatementKind<'source> {
        &self.kind
    }

    pub fn semicolon(&self) -> &Token<'source> {
        &self.semicolon
    }
}

impl<'source> Node<'source> for Statement<'source> {
    fn location(&self) -> Location<'source> {
        Location::new(
            self.semicolon.location().source(),
            Range::new(
                match &self.kind {
                    StatementKind::Import(import) => import.location().range().start(),
                    StatementKind::Expression(expression) => expression.location().range().start(),
                },
                self.semicolon.location().range().end(),
            ),
        )
    }
}
